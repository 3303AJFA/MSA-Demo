using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Менеджер поля-загона. Срез 1 — каркас: граф 5 узлов, выбор соседа кликом, перемещение
/// метки, счётчик ходов. БЕЗ типов, перетасовки, босса, связи с боем — в следующих срезах.
///
/// Граф рёбер хранится центрально (List&lt;HexEdge&gt;), bidirectional «бесплатно» — пара
/// узлов в одном edge'е значит соседство в обе стороны. Узлы про граф не знают.
///
/// Клик детектится централизованно: Update → Mouse.leftButton → Camera.ScreenPointToRay →
/// Physics.Raycast → HexFieldNode. UI блокирует клик через EventSystem.IsPointerOverGameObject.
/// </summary>
public class HexFieldManager : MonoBehaviour
{
    public static HexFieldManager Instance;

    [Header("Graph")]
    [Tooltip("Все узлы поля. Перетащить руками или собрать GetComponentsInChildren в Start (см. autoCollectNodes).")]
    public List<HexFieldNode> nodes = new List<HexFieldNode>();

    [Tooltip("Рёбра графа — пары узлов. Поле акта 1: 8 рёбер (4 спицы центр-угол + 4 периметр).")]
    public List<HexEdge> edges = new List<HexEdge>();

    [Tooltip("Стартовый узел. Обычно центр.")]
    public HexFieldNode startNode;

    [Tooltip("Если включено, в Awake собирает nodes через GetComponentsInChildren — если все гексы лежат под этим GO.")]
    public bool autoCollectNodes = false;

    [Header("Player")]
    [Tooltip("Метка игрока. Перемещается на наступленный узел.")]
    public PlayerMarker playerMarker;

    [Header("Input")]
    [Tooltip("Камера для raycast'а клика. Если null — Camera.main.")]
    public Camera fieldCamera;

    [Tooltip("Layer mask для гексов. -1 (Everything) — нормально для среза 1.")]
    public LayerMask hexLayerMask = ~0;

    [Tooltip("Максимальная дальность raycast'а.")]
    public float raycastDistance = 100f;

    [Header("Type Colors")]
    [Tooltip("Базовый цвет каждого типа узла. Состояние (current/available/disabled) модулирует поверх.")]
    public Color colorGeneric = new Color(0.60f, 0.60f, 0.60f);
    public Color colorBattle = new Color(0.85f, 0.25f, 0.25f);
    public Color colorChest = new Color(0.95f, 0.78f, 0.30f);
    public Color colorMerchant = new Color(0.30f, 0.55f, 0.90f);
    public Color colorEvent = new Color(0.65f, 0.40f, 0.80f);
    public Color colorCampfire = new Color(0.95f, 0.55f, 0.25f);

    [Tooltip("Цвет захваченной боссом клетки (срез 4). Перекрывает цвет типа, ещё модулируется состоянием.")]
    public Color colorCaptured = new Color(0.70f, 0.05f, 0.05f);

    [Header("UI")]
    [Tooltip("Debug-текст счётчика ходов. Заготовка под таймер N до босса в следующих срезах.")]
    public TextMeshProUGUI turnCountText;

    public HexFieldNode CurrentNode { get; private set; }
    public int TurnCount { get; private set; }

    /// <summary>
    /// True после того как Start завершил инициализацию поля и стрельнул OnFieldInitialized.
    /// Для подписчиков, которые могли стартовать позже менеджера.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Стреляет когда игрок наступил на узел. Точка подписки для будущих систем
    /// (босс-наступление, перетасовка пула, event-trigger) — без правки менеджера.
    /// Подписчики могут МЕНЯТЬ типы узлов; RefreshVisuals вызывается ПОСЛЕ event'а.
    /// </summary>
    public event System.Action<HexFieldNode> OnNodeEntered;

    /// <summary>
    /// Стреляет один раз в конце Start, после расстановки CurrentNode/marker, перед
    /// первым RefreshVisuals. Срез 3 — точка подписки для шафлера: рассыпать стартовые
    /// типы из пула. Подписчики могут менять типы; RefreshVisuals вызывается следом.
    /// </summary>
    public event System.Action OnFieldInitialized;

    /// <summary>
    /// Стреляет в конце MoveTo (после OnNodeEntered + RefreshVisuals). Срез 4 — точка
    /// хук-момента для босса: он берёт свой ход уже после того как шафлер перетасовал
    /// и менеджер отрисовал. Параметр — узел, куда наступил игрок.
    /// </summary>
    public event System.Action<HexFieldNode> OnPlayerTurnComplete;

    /// <summary>
    /// Счётчик блоков на ввод. Боссы/катсцены/анимации зовут PushInputBlock перед началом
    /// и PopInputBlock в OnComplete твина. Пока > 0 — клики игрока игнорируются.
    /// </summary>
    public bool InputBlocked => inputBlockers > 0;
    private int inputBlockers;
    public void PushInputBlock() { inputBlockers++; }
    public void PopInputBlock() { inputBlockers = Mathf.Max(0, inputBlockers - 1); }

    void Awake()
    {
        Instance = this;

        if (autoCollectNodes)
        {
            var collected = GetComponentsInChildren<HexFieldNode>(true);
            nodes.Clear();
            nodes.AddRange(collected);
        }
    }

    void Start()
    {
        if (startNode == null && nodes.Count > 0) startNode = nodes[0];

        CurrentNode = startNode;
        if (playerMarker != null && CurrentNode != null)
            playerMarker.SnapTo(CurrentNode.transform.position);

        UpdateTurnCountUI();

        // Подписчики (шафлер) могут поменять типы узлов на старте — отрисовываем после них.
        OnFieldInitialized?.Invoke();
        IsInitialized = true;

        RefreshVisuals();
    }

    void Update()
    {
        if (InputBlocked) return;  // босс анимирует ход / катсцена / etc.
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // UI ловит клик первой.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = fieldCamera != null ? fieldCamera : Camera.main;
        if (cam == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, hexLayerMask)) return;

        var node = hit.collider.GetComponent<HexFieldNode>();
        if (node == null) node = hit.collider.GetComponentInParent<HexFieldNode>();
        if (node != null) HandleNodeClick(node);
    }

    public Color GetTypeColor(HexNodeType type)
    {
        return type switch
        {
            HexNodeType.Battle => colorBattle,
            HexNodeType.Chest => colorChest,
            HexNodeType.Merchant => colorMerchant,
            HexNodeType.Event => colorEvent,
            HexNodeType.Campfire => colorCampfire,
            _ => colorGeneric
        };
    }

    public IEnumerable<HexFieldNode> GetNeighbors(HexFieldNode node)
    {
        if (node == null) yield break;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e == null) continue;
            if (e.a == node && e.b != null) yield return e.b;
            else if (e.b == node && e.a != null) yield return e.a;
        }
    }

    public bool IsNeighbor(HexFieldNode node)
    {
        if (node == null || CurrentNode == null) return false;
        foreach (var n in GetNeighbors(CurrentNode))
            if (n == node) return true;
        return false;
    }

    public void HandleNodeClick(HexFieldNode clicked)
    {
        if (clicked == null || clicked == CurrentNode) return;
        if (!IsNeighbor(clicked)) return;

        if (clicked.isCaptured)
        {
            Debug.Log($"[HexFieldManager] BOSS FIGHT — клик на захваченной {clicked.name} — TODO срез 5: SceneFlow.GoToBattle(boss)");
            return;
        }

        MoveTo(clicked);
    }

    private void MoveTo(HexFieldNode node)
    {
        CurrentNode = node;
        if (playerMarker != null) playerMarker.MoveTo(node.transform.position);

        TurnCount++;
        UpdateTurnCountUI();

        node.OnEntered();
        // Подписчики (шафлер) могут поменять типы узлов — отрисовываем после них.
        OnNodeEntered?.Invoke(node);

        RefreshVisuals();

        // Босс берёт свой ход уже после shuffle + refresh.
        OnPlayerTurnComplete?.Invoke(node);
    }

    public void RefreshVisuals()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            if (n == CurrentNode) n.SetVisual(HexNodeState.Current);
            else if (IsNeighbor(n)) n.SetVisual(HexNodeState.Available);
            else n.SetVisual(HexNodeState.Disabled);
        }
    }

    private void UpdateTurnCountUI()
    {
        if (turnCountText != null) turnCountText.text = $"Turn: {TurnCount}";
    }
}

[System.Serializable]
public class HexEdge
{
    public HexFieldNode a;
    public HexFieldNode b;
}
