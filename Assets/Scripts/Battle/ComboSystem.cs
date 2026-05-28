using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    [Header("Combo Settings")]
    public int maxComboSize = 6;

    [Header("Input Cooldown")]
    [Tooltip("Минимум beats между нажатиями одной и той же карты")]
    public float inputCooldownBeats = 0.5f;

    [Header("Rhythm Windows (в долях такта)")]
    [Range(0f, 1f)] public float perfectWindow = 0.15f;
    [Range(0f, 1f)] public float goodWindow = 0.30f;
    public float perfectBonus = 1.5f;
    public float goodBonus = 1.25f;

    private List<CardData> queuedCards = new List<CardData>();
    private Dictionary<CardData, float> lastInputPerCard = new Dictionary<CardData, float>();

    private bool inRhythmHitThisBeat;
    private bool subscribed;

    public bool IsCharging => queuedCards.Count > 0;
    public float RingProgress { get; private set; }

    void Awake() => Instance = this;

    void Start() => Subscribe();

    void Subscribe()
    {
        if (!subscribed && BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat += OnBeatReceived;
            subscribed = true;
        }
    }

    void OnDisable()
    {
        if (subscribed && BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= OnBeatReceived;
            subscribed = false;
        }
    }

    void Update()
    {
        if (queuedCards.Count > 0 && Keyboard.current.spaceKey.wasPressedThisFrame)
            FireComboManual();

        UpdateRingProgress();
    }

    void UpdateRingProgress()
    {
        if (!IsCharging)
        {
            RingProgress = 0f;
            return;
        }

        float spb = BeatManager.Instance.SecondsPerBeat;
        float timeSinceLastBeat = Time.time - BeatManager.Instance.LastBeatTime;
        RingProgress = Mathf.Clamp01(timeSinceLastBeat / spb);
    }

    void OnBeatReceived(int beatNumber)
    {
        if (queuedCards.Count == 0) return;

        if (!inRhythmHitThisBeat)
        {
            RhythmFeedback.Instance?.Show("SLEEPY HEAD", new Color(0.6f, 0.4f, 0.4f));
            FireCombo(1.0f);
        }

        inRhythmHitThisBeat = false;
    }

    public void OnCardPressed(CardData card)
    {
        if (queuedCards.Count >= maxComboSize)
        {
            Debug.Log("Combo limit reached!");
            return;
        }

        float cooldown = inputCooldownBeats * BeatManager.Instance.SecondsPerBeat;
        if (lastInputPerCard.TryGetValue(card, out float lastTime) && Time.time - lastTime < cooldown)
            return;

        lastInputPerCard[card] = Time.time;

        float offset = BeatManager.Instance.GetBeatOffset();
        EvaluateCardRhythm(offset);

        queuedCards.Add(card);
        AudioSystem.Instance?.PlayPick();
        ComboWindowUI.Instance?.AddCard(card);
        Debug.Log($"Queued: {card.cardName} ({queuedCards.Count}/{maxComboSize}) offset={offset:F2}");
    }

    void EvaluateCardRhythm(float offset)
    {
        if (offset < perfectWindow)
        {
            inRhythmHitThisBeat = true;
            RhythmFeedback.Instance?.Show("PERFECT!", new Color(1f, 0.85f, 0.2f));
        }
        else if (offset < goodWindow)
        {
            inRhythmHitThisBeat = true;
            RhythmFeedback.Instance?.Show("GOOD!", new Color(0.5f, 0.85f, 1f));
        }
        else
        {
            RhythmFeedback.Instance?.Show("OFF", new Color(0.5f, 0.5f, 0.5f));
        }
    }

    void FireComboManual()
    {
        float beatOffset = BeatManager.Instance.GetBeatOffset();

        float rhythmBonus = 1.0f;
        string rating = "OFF-BEAT";
        Color color = new Color(0.6f, 0.6f, 0.6f);

        if (beatOffset < perfectWindow)
        {
            rhythmBonus = perfectBonus;
            rating = "PERFECT!";
            color = new Color(1f, 0.85f, 0.2f);
        }
        else if (beatOffset < goodWindow)
        {
            rhythmBonus = goodBonus;
            rating = "GOOD!";
            color = new Color(0.5f, 0.85f, 1f);
        }

        RhythmFeedback.Instance?.Show(rating, color);
        Debug.Log($"♪ Fire: {rating} (offset {beatOffset:F2}, bonus x{rhythmBonus})");

        FireCombo(rhythmBonus);
    }

    void FireCombo(float rhythmBonus)
    {
        if (queuedCards.Count == 0) return;

        float totalCost = 0f;
        foreach (var card in queuedCards)
            totalCost += card.voltageCost;

        if (!BattleManager.Instance.TrySpendVoltage(totalCost))
        {
            Debug.Log("Not enough Voltage!");
            ClearQueue();
            return;
        }

        AudioSystem.Instance?.PlayCombo(new List<CardData>(queuedCards));
        BattleManager.Instance.lastRhythmBonus = rhythmBonus;

        foreach (var card in queuedCards)
            BattleManager.Instance.ExecuteCard(card);

        Debug.Log($"=== Combo fired ({queuedCards.Count} cards, rhythm x{rhythmBonus}) ===");
        ClearQueue();
    }

    void ClearQueue()
    {
        queuedCards.Clear();
        ComboWindowUI.Instance?.ClearSlots();
        inRhythmHitThisBeat = false;
    }

    public bool HasCard(CardData card) => queuedCards.Contains(card);
    public float GetChargeProgress() => RingProgress;
}
