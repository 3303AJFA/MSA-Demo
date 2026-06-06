using UnityEngine;

/// <summary>
/// Перетасовка типов узлов после каждого хода. Срез 3 — единое правило:
///
///   - Текущий узел (где стоит игрок) → Generic (пустой/израсходованный). Эффект отыгрался
///     в OnEntered, узел вычерпан, пока игрок на нём стоит.
///   - Все остальные узлы → свежий случайный тип из пула (Battle/Chest/Merchant/Event/Campfire).
///     Бывший «текущий» прошлого хода ОЖИВАЕТ — уходит в общую перетасовку, получает рандом.
///
/// Пуста всегда ровно ОДНА клетка — та, под игроком. Generic не связан с previousNode.
/// Старт — частный случай того же правила: игрок на startNode → startNode = Generic
/// автоматически. Никакого init-трюка не нужно.
///
/// Анти-фарм держится двойной защитой: (а) стоя на узле он пуст; (б) уйдя и вернувшись —
/// узел уже перетасован в случайный тип (не свой прежний).
///
/// Пул для рандома — 5 типов БЕЗ Generic. Generic зарезервирован для текущего узла.
/// Чистый рандом, равные веса (по CLAUDE.local — стартовая позиция, тюнинг по плейтесту).
///
/// Компонент не трогает менеджер — только подписывается на его события. Можно класть как на
/// тот же GO, так и на отдельный, лишь бы поле manager было привязано в инспекторе.
/// </summary>
public class HexNodeShuffler : MonoBehaviour
{
    [Tooltip("HexFieldManager — перетащить в инспекторе. Шафлер подписывается на его события.")]
    public HexFieldManager manager;

    [Tooltip("Сид RNG для воспроизводимости (например, в тестах). 0 — несид, чистая случайность.")]
    public int seed = 0;

    // Пул типов для перетасовки. Generic НЕ входит — он только для пустого пройденного узла.
    private static readonly HexNodeType[] Pool = new HexNodeType[]
    {
        HexNodeType.Battle,
        HexNodeType.Chest,
        HexNodeType.Merchant,
        HexNodeType.Event,
        HexNodeType.Campfire
    };

    private System.Random rng;

    void Awake()
    {
        rng = seed != 0 ? new System.Random(seed) : new System.Random();
    }

    void OnEnable()
    {
        if (manager == null)
        {
            Debug.LogError("[HexNodeShuffler] manager не привязан в инспекторе — шафлер выключен.");
            enabled = false;
            return;
        }
        manager.OnFieldInitialized += HandleFieldInitialized;
        manager.OnNodeEntered += HandleNodeEntered;

        // На случай если шафлер enable'нулся ПОСЛЕ того как менеджер уже инициализировался
        // (поздний spawn, scene reload и т.п.) — догнать вручную.
        if (manager.IsInitialized)
        {
            HandleFieldInitialized();
            manager.RefreshVisuals();
        }
    }

    void OnDisable()
    {
        if (manager == null) return;
        manager.OnFieldInitialized -= HandleFieldInitialized;
        manager.OnNodeEntered -= HandleNodeEntered;
    }

    private void HandleFieldInitialized() => Shuffle();
    private void HandleNodeEntered(HexFieldNode entered) => Shuffle();

    private void Shuffle()
    {
        var current = manager.CurrentNode;
        var nodes = manager.nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            if (n.isCaptured) continue;  // захваченные боссом не тасуем (срез 4)

            n.nodeType = (n == current)
                ? HexNodeType.Generic
                : Pool[rng.Next(Pool.Length)];
        }
    }
}
