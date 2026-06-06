using UnityEngine;

/// <summary>
/// Узел поля-загона. Срез 2 — 5 типов узлов (Battle/Chest/Merchant/Event/Campfire) + Generic
/// как дефолт. Цвет узла = базовый цвет ТИПА (от HexFieldManager) × модуляция по СОСТОЯНИЮ:
/// Current = Lerp в белый (подсветка), Available = базовый, Disabled = умножение на 0.35
/// (затемнение). Игрок видит И что это за узел, И доступен ли он.
///
/// OnEntered() пока всё ещё Debug-заглушка, но развёрнут switch по типу — точка диспатча
/// в реальные эффекты (срез 5+: бой, костёр, торговец, событие, сундук).
///
/// Узлу нужен любой Collider (на GO или ребёнке) для raycast'а в HexFieldManager.
/// </summary>
public class HexFieldNode : MonoBehaviour
{
    [Tooltip("Произвольный ID для логов / будущего save. На логику не влияет.")]
    public int nodeID;

    [Tooltip("Тип узла. Срез 2 — пользователь проставляет руками. Срез 3 — автоматический пул-перетасовка.")]
    public HexNodeType nodeType = HexNodeType.Generic;

    [Tooltip("Захвачен боссом (срез 4). Красится capturedColor от Manager поверх типа, шафлер пропускает, " +
             "клик игрока = BOSS FIGHT. Управляется BossController, не лезть руками.")]
    public bool isCaptured;

    [Tooltip("Renderer гекса. Если null — берётся первый Renderer в дереве GO.")]
    public Renderer hexRenderer;

    [Tooltip("Имя property цвета в шейдере. URP Lit = '_BaseColor', Built-in = '_Color'.")]
    public string colorPropertyName = "_BaseColor";

    [Tooltip("Цвет если HexFieldManager недоступен (edit-mode превью, etc).")]
    public Color fallbackColor = new Color(0.5f, 0.5f, 0.5f);

    public HexNodeState VisualState { get; private set; } = HexNodeState.Disabled;

    private MaterialPropertyBlock mpb;
    private int colorPropertyID;

    void Awake()
    {
        if (hexRenderer == null) hexRenderer = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        colorPropertyID = Shader.PropertyToID(colorPropertyName);
    }

    public void SetVisual(HexNodeState state)
    {
        VisualState = state;
        if (hexRenderer == null) return;

        // Захват приоритетнее типа — клетка красная независимо от nodeType, но всё ещё
        // модулируется по состоянию (current/available/disabled) чтобы было видно,
        // достанется ли игрок этой красной клетки на следующем ходу.
        Color baseColor = isCaptured ? GetCapturedColor() : GetBaseColor();
        Color final = state switch
        {
            HexNodeState.Current => Color.Lerp(baseColor, Color.white, 0.35f),
            HexNodeState.Available => baseColor,
            _ => baseColor * 0.35f
        };
        final.a = baseColor.a;

        hexRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorPropertyID, final);
        hexRenderer.SetPropertyBlock(mpb);
    }

    private Color GetBaseColor()
    {
        var mgr = HexFieldManager.Instance;
        return mgr != null ? mgr.GetTypeColor(nodeType) : fallbackColor;
    }

    private Color GetCapturedColor()
    {
        var mgr = HexFieldManager.Instance;
        return mgr != null ? mgr.colorCaptured : new Color(0.70f, 0.05f, 0.05f);
    }

    /// <summary>
    /// Игрок наступил на узел. Срез 2 — Debug.Log с типом. Срез 5+ — в каждый case
    /// ложится настоящая логика (запуск боя / костёр-выбор / торговец / событие через
    /// Yarn|EventOverlay / награда сундука). Структура switch'а — чтобы расширять не задумываясь.
    /// </summary>
    public void OnEntered()
    {
        switch (nodeType)
        {
            case HexNodeType.Battle:
                Debug.Log($"[HexFieldNode #{nodeID}] Battle — TODO срез 5: SceneFlow.GoToBattle()");
                break;
            case HexNodeType.Chest:
                Debug.Log($"[HexFieldNode #{nodeID}] Chest — TODO срез 5: дать реликвию/предмет");
                break;
            case HexNodeType.Merchant:
                Debug.Log($"[HexFieldNode #{nodeID}] Merchant — TODO срез 5: открыть магазин");
                break;
            case HexNodeType.Event:
                Debug.Log($"[HexFieldNode #{nodeID}] Event — TODO срез 5: запустить EventOverlay/Yarn");
                break;
            case HexNodeType.Campfire:
                Debug.Log($"[HexFieldNode #{nodeID}] Campfire — TODO срез 5: выбор Heal vs Upgrade");
                break;
            case HexNodeType.Generic:
            default:
                Debug.Log($"[HexFieldNode #{nodeID}] Generic — placeholder, тип не назначен");
                break;
        }
    }
}

/// <summary>
/// Порядок значений — Unity сериализует по индексу. НЕ переставлять существующие
/// значения, новые — ТОЛЬКО в конец.
/// </summary>
public enum HexNodeType
{
    Generic = 0,
    Battle = 1,
    Chest = 2,
    Merchant = 3,
    Event = 4,
    Campfire = 5
}

public enum HexNodeState
{
    Disabled,
    Available,
    Current
}
