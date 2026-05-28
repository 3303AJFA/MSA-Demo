using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance;

    [Header("Rhythm")]
    public float bpm = 100f;

    public float SecondsPerBeat => 60f / bpm;

    public float CurrentBeatTime { get; private set; }
    public int CurrentBeat { get; private set; }

    // Time.time когда сработала последняя доля. До первой доли = 0.
    public float LastBeatTime { get; private set; }

    // Событие — другие системы могут подписаться на удары
    public delegate void BeatEvent(int beatNumber);
    public event BeatEvent OnBeat;

    private float timer = 0f;

    void Awake()
    {
        Instance = this;
        LastBeatTime = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        CurrentBeatTime = timer % SecondsPerBeat;

        if (timer >= SecondsPerBeat)
        {
            timer -= SecondsPerBeat;
            CurrentBeat++;
            LastBeatTime = Time.time;
            OnBeat?.Invoke(CurrentBeat);
            Debug.Log($"♪ Beat {CurrentBeat}");
        }
    }

    // Насколько мы близко к доле (0 = точно на ноте, 1 = середина между долями)
    public float GetBeatOffset()
    {
        float halfBeat = SecondsPerBeat / 2f;
        float offset = Mathf.Min(CurrentBeatTime, SecondsPerBeat - CurrentBeatTime);
        return offset / halfBeat; // нормализованный 0-1
    }

    // Возвращает Time.time ближайшей будущей доли, не раньше desiredTime + minLookahead.
    // Используется ComboSystem чтобы "защёлкнуть" TargetTime на метроном.
    public float SnapToNextBeat(float desiredTime, float minLookahead = 0.05f)
    {
        float spb = SecondsPerBeat;
        // Сколько долей от LastBeatTime до желаемого момента (с учётом небольшого зазора)
        float dt = (desiredTime + minLookahead) - LastBeatTime;
        int beats = Mathf.Max(1, Mathf.CeilToInt(dt / spb));
        return LastBeatTime + beats * spb;
    }
}