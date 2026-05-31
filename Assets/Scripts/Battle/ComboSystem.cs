using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    [Header("Combo Settings")]
    public int maxComboSize = 6;

    [Header("Input Cooldown")]
    [Tooltip("Глобальный fallback кулдаун в долях такта, если у CardData.cooldownBeats == 0")]
    public float defaultCooldownBeats = 0.5f;

    [Header("Riff Decay")]
    [Tooltip("Через сколько бит после последней ноты комбо авто-отпускается со стандартным множителем (1.0)")]
    public float comboDecayBeats = 2f;

    [Header("Release Tiers (в долях такта)")]
    [Range(0f, 1f)] public float perfectWindow = 0.15f;
    [Range(0f, 1f)] public float goodWindow = 0.30f;

    [Tooltip("Множитель урона при PERFECT отпуске (offset < perfectWindow)")]
    public float perfectBonus = 1.5f;

    [Tooltip("LEGACY поле, в новой рифовой модели не используется. GOOD теперь даёт стандарт 1.0.")]
    public float goodBonus = 1.25f;

    [Tooltip("Множитель при ручном отпуске вне goodWindow — мягкий штраф за зевание")]
    public float offBeatPenalty = 0.85f;

    [Header("Streak (DMC-style)")]
    [Tooltip("Веса изменения стрика. Меняй здесь, не в коде.")]
    public StreakProfile streakProfile = new StreakProfile();

    [Tooltip("Временный дебаг-вывод стрика (rank + значение). Привязать TextMeshProUGUI на Canvas.")]
    [SerializeField] private TextMeshProUGUI streakDebugText;

    public float StreakValue { get; private set; }

    private List<CardData> queuedCards = new List<CardData>();

    // Все таймеры теперь в позициях SongPositionInBeats, не в Time.time
    private Dictionary<CardData, float> lastInputPerCard = new Dictionary<CardData, float>();
    private Dictionary<CardData, float> cooldownOverrides = new Dictionary<CardData, float>();
    private float lastNoteBeatPos = 0f;

    public bool IsCharging => queuedCards.Count > 0;
    public float RingProgress { get; private set; }

    void Awake() => Instance = this;

    /// <summary>
    /// Чистый лист на старте боя. Очищает очередь, кулдауны, стрик, визуал.
    /// Вызывается из BattleManager.StartBattle() — единая точка инициализации.
    /// </summary>
    public void ResetForBattle()
    {
        queuedCards.Clear();
        lastInputPerCard.Clear();
        cooldownOverrides.Clear();  // если предметы дают override — они должны переустановиться на старте боя
        lastNoteBeatPos = 0f;
        RingProgress = 0f;
        ResetStreak();

        ComboWindowUI.Instance?.ClearSlots();
        MultiplierIndicator.Instance?.OnComboFired();
    }

    void Update()
    {
        if (queuedCards.Count > 0 && Keyboard.current.spaceKey.wasPressedThisFrame)
            FireComboManual();

        UpdateRingProgress();
        CheckComboDecay();
    }

    void UpdateRingProgress()
    {
        if (!IsCharging || BeatManager.Instance == null)
        {
            RingProgress = 0f;
            return;
        }

        // Прогресс кольца — дробная часть SongPositionInBeats.
        // Один цикл кольца = один бит, синхронно с метрономом.
        float songPos = BeatManager.Instance.SongPositionInBeats;
        RingProgress = songPos - Mathf.Floor(songPos);
    }

    void CheckComboDecay()
    {
        if (queuedCards.Count == 0 || BeatManager.Instance == null) return;

        float songPos = BeatManager.Instance.SongPositionInBeats;
        if (songPos - lastNoteBeatPos >= comboDecayBeats)
        {
            // Авто-отпуск рифа: игрок зевнул отпуск.
            // Множитель урона остаётся стандартный 1.0 (не дебафф) — это решено в рифовой модели.
            // Но стрик штрафуется отдельно: зевок ритма не бесплатен.
            RhythmFeedback.Instance?.Show("SLEEPY HEAD", new Color(0.6f, 0.4f, 0.4f));
            AddStreak(-streakProfile.sleepyLoss);
            FireCombo(1.0f);
        }
    }

    public bool OnCardPressed(CardData card)
    {
        if (queuedCards.Count >= maxComboSize)
        {
            Debug.Log("Combo limit reached!");
            return false;
        }

        if (BeatManager.Instance == null) return false;
        float songPos = BeatManager.Instance.SongPositionInBeats;

        // Per-card кулдаун — теперь в битах, не в секундах
        float cooldown = GetCooldownBeats(card);
        if (lastInputPerCard.TryGetValue(card, out float lastBeatPos) && songPos - lastBeatPos < cooldown)
            return false;

        lastInputPerCard[card] = songPos;
        lastNoteBeatPos = songPos;

        // Оценка ноты: visual feedback + изменение стрика (стрик растёт по-нотно в реальном времени).
        // На урон влияет: streak multiplier (за всю боёвку) × release multiplier (при пробеле).
        float offset = BeatManager.Instance.GetBeatOffset();
        EvaluateCardRhythm(offset);

        queuedCards.Add(card);
        AudioSystem.Instance?.PlayPick();
        ComboWindowUI.Instance?.AddCard(card);
        Debug.Log($"Queued: {card.cardName} ({queuedCards.Count}/{maxComboSize}) songPos={songPos:F2} offset={offset:F2}");
        return true;
    }

    void EvaluateCardRhythm(float offset)
    {
        if (offset < perfectWindow)
        {
            RhythmFeedback.Instance?.Show("PERFECT!", new Color(1f, 0.85f, 0.2f));
            AddStreak(streakProfile.perfectGain);
        }
        else if (offset < goodWindow)
        {
            RhythmFeedback.Instance?.Show("GOOD!", new Color(0.5f, 0.85f, 1f));
            AddStreak(streakProfile.goodGain);
        }
        else
        {
            RhythmFeedback.Instance?.Show("OFF", new Color(0.5f, 0.5f, 0.5f));
            AddStreak(-streakProfile.offLoss);
        }
    }

    void FireComboManual()
    {
        if (BeatManager.Instance == null) return;
        float offset = BeatManager.Instance.GetBeatOffset();

        float releaseMultiplier;
        string rating;
        Color color;

        if (offset < perfectWindow)
        {
            releaseMultiplier = perfectBonus;
            rating = "PERFECT!";
            color = new Color(1f, 0.85f, 0.2f);
        }
        else if (offset < goodWindow)
        {
            releaseMultiplier = 1.0f; // стандарт, без бонуса
            rating = "GOOD!";
            color = new Color(0.5f, 0.85f, 1f);
        }
        else
        {
            releaseMultiplier = offBeatPenalty; // мягкий дебафф за зевание
            rating = "OFF-BEAT";
            color = new Color(0.6f, 0.6f, 0.6f);
        }

        RhythmFeedback.Instance?.Show(rating, color);
        Debug.Log($"♪ Release: {rating} (offset {offset:F2}, mult x{releaseMultiplier})");

        FireCombo(releaseMultiplier);
    }

    void FireCombo(float releaseMultiplier)
    {
        if (queuedCards.Count == 0) return;

        float totalCost = 0f;
        foreach (var card in queuedCards)
            totalCost += card.GetVoltageCost();

        if (!BattleManager.Instance.TrySpendVoltage(totalCost))
        {
            Debug.Log("Not enough Voltage!");
            ClearQueue();
            return;
        }

        AudioSystem.Instance?.PlayCombo(new List<CardData>(queuedCards));

        // Финальный множитель: streak (за всю боёвку) × release (приземление пробелом)
        float streakMult = GetStreakMultiplier();
        float finalMult = streakMult * releaseMultiplier;
        BattleManager.Instance.lastRhythmBonus = finalMult;

        foreach (var card in queuedCards)
            BattleManager.Instance.ExecuteCard(card);

        Debug.Log($"=== Combo fired ({queuedCards.Count} cards, streak x{streakMult:F2} × release x{releaseMultiplier:F2} = x{finalMult:F2}) ===");
        ClearQueue();
    }

    void ClearQueue()
    {
        queuedCards.Clear();
        ComboWindowUI.Instance?.ClearSlots();
        MultiplierIndicator.Instance?.OnComboFired();
    }

    public bool HasCard(CardData card) => queuedCards.Contains(card);
    public float GetChargeProgress() => RingProgress;

    public float GetCooldownBeats(CardData card)
    {
        if (cooldownOverrides.TryGetValue(card, out float over)) return over;
        if (card != null)
        {
            float cd = card.GetCooldownBeats();
            if (cd > 0f) return cd;
        }
        return defaultCooldownBeats;
    }

    public void SetCooldownOverride(CardData card, float beats) => cooldownOverrides[card] = beats;
    public void ClearCooldownOverride(CardData card) => cooldownOverrides.Remove(card);

    // --- Streak (DMC-style) ---

    /// <summary>
    /// Единая точка изменения стрика. Клампит в [0, 100], обновляет дебаг-вывод.
    /// </summary>
    public void AddStreak(float delta)
    {
        StreakValue = Mathf.Clamp(StreakValue + delta, 0f, 100f);
        UpdateStreakDebugText();
        Debug.Log($"♪ Streak: {GetRank()} {StreakValue:F0} (delta {delta:+0;-0})");
    }

    /// <summary>
    /// Обнулить стрик. Вызывается на старте боя, Victory, Defeat.
    /// </summary>
    public void ResetStreak()
    {
        StreakValue = 0f;
        UpdateStreakDebugText();
    }

    /// <summary>
    /// Линейный множитель урона: streak=0 → x1.0, streak=100 → x2.0.
    /// </summary>
    public float GetStreakMultiplier() => Mathf.Lerp(1.0f, 2.0f, StreakValue / 100f);

    /// <summary>
    /// Буквенный рейтинг по StreakValue. Пороги: D 0-14, C 15-34, B 35-54, A 55-74, S 75-89, SS 90-99, SSS 100.
    /// </summary>
    public string GetRank()
    {
        float v = StreakValue;
        if (v >= 100f) return "SSS";
        if (v >= 90f) return "SS";
        if (v >= 75f) return "S";
        if (v >= 55f) return "A";
        if (v >= 35f) return "B";
        if (v >= 15f) return "C";
        return "D";
    }

    void UpdateStreakDebugText()
    {
        if (streakDebugText != null)
            streakDebugText.text = $"{GetRank()}  {StreakValue:F0}";
    }
}
