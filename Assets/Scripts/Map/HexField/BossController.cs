using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Босс поля-загона. Срез 4 — приходит на ходу N, захватывает клетки красным,
/// загоняет игрока BFS-алгоритмом. Контакт с захваченной = пока Debug.Log (срез 5
/// заменит на SceneFlow.GoToBattle).
///
/// АРХИТЕКТУРА: компонент НЕ трогает менеджер, только подписывается на его события
/// (OnFieldInitialized / OnPlayerTurnComplete). Менеджер про босса не знает.
///
/// АЛГОРИТМ ХОДА (после хода игрока):
///   1. Свободные соседи капсулы → выбрать ближайшего к игроку по BFS-расстоянию,
///      шагнуть туда + ЗАХВАТИТЬ (новая красная).
///   2. Иначе (все соседи капсулы красные или клетка игрока) → шаг по красным
///      на первый узел кратчайшего пути к ближайшей свободной (BFS через captured,
///      минуя игрока). Этот шаг НЕ красит — логистика.
///   3. Иначе → пропустить ход (единственная свободная под игроком, ждём шага игрока).
///
/// Босс НИКОГДА не заходит на клетку игрока. Контакт инициирует игрок шагом на красную.
///
/// ВИЗУАЛ: использует manager.turnCountText как общий debug-таймер. До прихода —
/// «Boss in: N». После прихода — «BOSS · captured X/Y». Anim-движение капсулы через
/// DOTween, на время хода манагер заблокирован через PushInputBlock.
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Менеджер поля. Перетащить руками.")]
    public HexFieldManager manager;

    [Tooltip("Капсула-заглушка босса. Стартует disabled в инспекторе, BossController энейблит на приходе.")]
    public GameObject bossCapsule;

    [Header("Arrival")]
    [Tooltip("Ход, на котором босс приходит. Акт 1 = 10 (по CLAUDE.local). Растёт с актом.")]
    public int bossArrivalTurn = 10;

    [Header("Visual")]
    [Tooltip("Смещение капсулы над клеткой.")]
    public Vector3 capsuleOffset = new Vector3(0f, 0.6f, 0f);

    [Tooltip("Пауза перед ходом босса — даёт игроку увидеть состояние поля до движения.")]
    public float thinkDelay = 0.25f;

    [Tooltip("Длительность твина капсулы между клетками.")]
    public float moveDuration = 0.4f;

    public bool HasArrived { get; private set; }
    public HexFieldNode CapsuleNode { get; private set; }

    void OnEnable()
    {
        if (manager == null)
        {
            Debug.LogError("[BossController] manager не привязан в инспекторе — босс выключен.");
            enabled = false;
            return;
        }
        manager.OnFieldInitialized += HandleFieldInitialized;
        manager.OnPlayerTurnComplete += HandlePlayerTurnComplete;

        // Догон поздней подписки — если менеджер уже стартовал до нас.
        if (manager.IsInitialized) HandleFieldInitialized();
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.OnFieldInitialized -= HandleFieldInitialized;
        manager.OnPlayerTurnComplete -= HandlePlayerTurnComplete;
    }

    private void HandleFieldInitialized()
    {
        if (bossCapsule != null) bossCapsule.SetActive(false);
        HasArrived = false;
        CapsuleNode = null;
        // Снять флаги захвата на всех узлах (на случай re-init / повтор play mode).
        for (int i = 0; i < manager.nodes.Count; i++)
        {
            if (manager.nodes[i] != null) manager.nodes[i].isCaptured = false;
        }
        UpdateBossUI();
    }

    private void HandlePlayerTurnComplete(HexFieldNode movedTo)
    {
        if (!HasArrived)
        {
            if (manager.TurnCount >= bossArrivalTurn) Arrive();
            else UpdateBossUI();
            return;
        }

        DoBossTurn();
    }

    // === Arrival ===

    private void Arrive()
    {
        var freeNodes = new List<HexFieldNode>();
        for (int i = 0; i < manager.nodes.Count; i++)
        {
            var n = manager.nodes[i];
            if (n == null) continue;
            if (n == manager.CurrentNode) continue;
            if (n.isCaptured) continue;
            freeNodes.Add(n);
        }

        if (freeNodes.Count == 0)
        {
            Debug.LogWarning("[BossController] Нет свободных клеток для прихода босса — пропускаю.");
            UpdateBossUI();
            return;
        }

        var spawnNode = freeNodes[Random.Range(0, freeNodes.Count)];
        spawnNode.isCaptured = true;
        CapsuleNode = spawnNode;
        HasArrived = true;

        if (bossCapsule != null)
        {
            bossCapsule.SetActive(true);
            bossCapsule.transform.position = spawnNode.transform.position + capsuleOffset;
        }

        manager.RefreshVisuals();
        UpdateBossUI();
        Debug.Log($"[BossController] Boss arrived at {spawnNode.name} on turn {manager.TurnCount}.");
    }

    // === Boss turn ===

    private void DoBossTurn()
    {
        var player = manager.CurrentNode;
        var capsule = CapsuleNode;
        if (capsule == null || player == null) return;

        // 1. Свободный сосед — шагнуть и захватить.
        var freeNeighbor = FindBestFreeNeighborTowardPlayer(capsule, player);
        if (freeNeighbor != null)
        {
            BossMoveAndCapture(freeNeighbor);
            return;
        }

        // 2. Шаг по красным к ближайшей свободной.
        var stepAlongCaptured = FindFirstStepThroughCaptured(capsule, player);
        if (stepAlongCaptured != null)
        {
            BossMoveOnly(stepAlongCaptured);
            return;
        }

        // 3. Wait. Единственная свободная — под игроком.
        Debug.Log("[BossController] Boss waits — единственная свободная клетка под игроком.");
        UpdateBossUI();
    }

    private void BossMoveAndCapture(HexFieldNode target)
    {
        target.isCaptured = true;
        CapsuleNode = target;
        manager.RefreshVisuals();  // новая красная сразу видна
        AnimateCapsuleTo(target);
    }

    private void BossMoveOnly(HexFieldNode target)
    {
        CapsuleNode = target;
        AnimateCapsuleTo(target);  // ничего нового не красим
    }

    private void AnimateCapsuleTo(HexFieldNode target)
    {
        if (bossCapsule == null)
        {
            UpdateBossUI();
            return;
        }

        manager.PushInputBlock();
        Vector3 targetPos = target.transform.position + capsuleOffset;

        bossCapsule.transform.DOKill();
        var seq = DOTween.Sequence();
        seq.AppendInterval(thinkDelay);
        seq.Append(bossCapsule.transform.DOMove(targetPos, moveDuration).SetEase(Ease.InOutQuad));
        seq.OnComplete(() =>
        {
            manager.RefreshVisuals();
            UpdateBossUI();
            manager.PopInputBlock();
        });
    }

    // === BFS helpers ===

    /// <summary>
    /// Среди свободных не-игроковых соседей капсулы — вернуть ближайшего к игроку по BFS
    /// (число рёбер графа). null если таких соседей нет.
    /// </summary>
    private HexFieldNode FindBestFreeNeighborTowardPlayer(HexFieldNode capsule, HexFieldNode player)
    {
        HexFieldNode best = null;
        int bestDist = int.MaxValue;
        foreach (var n in manager.GetNeighbors(capsule))
        {
            if (n == null) continue;
            if (n.isCaptured) continue;
            if (n == player) continue;
            int d = BfsDistance(n, player);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }
        return best;
    }

    private int BfsDistance(HexFieldNode from, HexFieldNode to)
    {
        if (from == null || to == null) return int.MaxValue;
        if (from == to) return 0;
        var visited = new HashSet<HexFieldNode> { from };
        var queue = new Queue<(HexFieldNode node, int dist)>();
        queue.Enqueue((from, 0));
        while (queue.Count > 0)
        {
            var (cur, dist) = queue.Dequeue();
            foreach (var n in manager.GetNeighbors(cur))
            {
                if (n == null) continue;
                if (visited.Contains(n)) continue;
                visited.Add(n);
                if (n == to) return dist + 1;
                queue.Enqueue((n, dist + 1));
            }
        }
        return int.MaxValue;
    }

    /// <summary>
    /// BFS от капсулы, минуя игрока. Найти ближайшую свободную клетку, вернуть ПЕРВЫЙ
    /// шаг на пути к ней от капсулы (соседа капсулы на пути). Этот шаг идёт по красным.
    /// null если ни одна свободная клетка не достижима.
    /// </summary>
    private HexFieldNode FindFirstStepThroughCaptured(HexFieldNode capsule, HexFieldNode player)
    {
        var visited = new HashSet<HexFieldNode> { capsule };
        var queue = new Queue<HexFieldNode>();
        var pred = new Dictionary<HexFieldNode, HexFieldNode>();
        queue.Enqueue(capsule);

        HexFieldNode found = null;
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var n in manager.GetNeighbors(cur))
            {
                if (n == null) continue;
                if (visited.Contains(n)) continue;
                if (n == player) continue;
                visited.Add(n);
                pred[n] = cur;
                if (!n.isCaptured)
                {
                    found = n;
                    break;
                }
                queue.Enqueue(n);
            }
            if (found != null) break;
        }

        if (found == null) return null;

        // Reconstruct path: walk back from found to capsule's direct neighbor.
        var cur2 = found;
        while (pred.TryGetValue(cur2, out var prev))
        {
            if (prev == capsule) return cur2;
            cur2 = prev;
        }
        return null;
    }

    // === UI ===

    private void UpdateBossUI()
    {
        if (manager.turnCountText == null) return;

        if (!HasArrived)
        {
            int remaining = bossArrivalTurn - manager.TurnCount;
            manager.turnCountText.text = remaining <= 0
                ? "BOSS INCOMING"
                : $"Boss in: {remaining}";
        }
        else
        {
            int captured = 0;
            for (int i = 0; i < manager.nodes.Count; i++)
            {
                if (manager.nodes[i] != null && manager.nodes[i].isCaptured) captured++;
            }
            manager.turnCountText.text = $"BOSS · captured {captured}/{manager.nodes.Count}";
        }
    }
}
