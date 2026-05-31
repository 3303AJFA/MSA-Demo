using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance;

    [Header("Rhythm")]
    public float bpm = 100f;

    public float SecondsPerBeat => 60f / bpm;

    // --- НОВЫЙ источник музыкального времени ---
    // AudioSettings.dspTime — стабильное время аудиосистемы, не дрейфует от фреймрейта
    // как Time.time. Всё тайминговое отныне — производное отсюда.
    private double dspStartTime;

    /// <summary>
    /// Единый источник тайминга. Дробная позиция на сетке битов.
    /// 4.0 = ровно на 4-м бите, 4.5 = посередине между 4 и 5.
    /// Все системы (ComboSystem, ComboWindowUI, BPMVisualizer) должны считать
    /// прогресс отсюда, а не от Time.time.
    /// </summary>
    public float SongPositionInBeats
    {
        get
        {
            double elapsed = AudioSettings.dspTime - dspStartTime;
            return (float)(elapsed / SecondsPerBeat);
        }
    }

    // --- LEGACY поля (оставлены для обратной совместимости) ---
    // Новый код должен использовать SongPositionInBeats.
    public float CurrentBeatTime { get; private set; }
    public int CurrentBeat { get; private set; }
    public float LastBeatTime { get; private set; }

    public delegate void BeatEvent(int beatNumber);
    public event BeatEvent OnBeat;

    private int lastBeatInt;

    void Awake()
    {
        Instance = this;
        dspStartTime = AudioSettings.dspTime;
        lastBeatInt = 0;
        CurrentBeat = 0;
        LastBeatTime = 0f;
    }

    void Update()
    {
        float songPos = SongPositionInBeats;
        int beatInt = Mathf.FloorToInt(songPos);

        // legacy CurrentBeatTime — для старого кода, который ещё считает в секундах
        float spb = SecondsPerBeat;
        float frac = songPos - beatInt;
        CurrentBeatTime = frac * spb;

        // OnBeat фаерится при пересечении целого бита
        if (beatInt > lastBeatInt)
        {
            CurrentBeat = beatInt;
            LastBeatTime = Time.time;
            OnBeat?.Invoke(CurrentBeat);
            Debug.Log($"♪ Beat {CurrentBeat} (songPos {songPos:F2})");
            lastBeatInt = beatInt;
        }
    }

    /// <summary>
    /// Нормализованная близость к ближайшему биту: 0 = точно на бите, 1 = середина между битами.
    /// Симметрично работает на «чуть раньше» и «чуть позже» — оба одинаково оцениваются.
    /// </summary>
    public float GetBeatOffset()
    {
        float songPos = SongPositionInBeats;
        float frac = songPos - Mathf.Floor(songPos);
        float halfDistance = Mathf.Min(frac, 1f - frac);
        return halfDistance * 2f; // 0..1
    }

    /// <summary>
    /// Снап к ближайшей будущей доле метронома (LEGACY — для старых вызовов через TargetTime в секундах).
    /// Новый код должен оперировать SongPositionInBeats напрямую.
    /// </summary>
    public float SnapToNextBeat(float desiredTime, float minLookahead = 0.05f)
    {
        float spb = SecondsPerBeat;
        float dt = (desiredTime + minLookahead) - LastBeatTime;
        int beats = Mathf.Max(1, Mathf.CeilToInt(dt / spb));
        return LastBeatTime + beats * spb;
    }

    /// <summary>
    /// Сменить BPM сохраняя непрерывность SongPositionInBeats.
    /// Используется между энкаунтерами или при темпо-модификаторах.
    /// dspStartTime пересчитывается так, чтобы текущая позиция в битах не прыгнула.
    /// </summary>
    public void SetBPM(float newBpm)
    {
        if (newBpm <= 0f) return;

        // Сохраняем текущую позицию ДО смены SecondsPerBeat
        float currentPos = SongPositionInBeats;

        bpm = newBpm;

        // Пересчёт: newDspStartTime такой, что (dspTime - newDspStartTime) / newSPB == currentPos
        dspStartTime = AudioSettings.dspTime - (double)(currentPos * SecondsPerBeat);

        Debug.Log($"♪ BPM set to {bpm} (continuity preserved at songPos {currentPos:F2})");
    }
}
