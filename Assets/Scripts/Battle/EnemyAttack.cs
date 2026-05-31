using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    public static EnemyAttack Instance;

    [Header("Attack Settings")]
    [Tooltip("Атаковать каждые N ударов метронома")]
    public int attackEveryBeats = 4;

    [Tooltip("Урон за атаку")]
    public int damagePerAttack = 8;

    [Tooltip("Сколько beats пропустить перед первой атакой")]
    public int warmupBeats = 2;

    private bool subscribed;

    void Awake() => Instance = this;

    void Start()
    {
        if (BeatManager.Instance != null)
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

    /// <summary>
    /// Детерминированный запрос: будет ли на этом бите вражеский удар?
    /// Единый источник истины — и для самого удара (OnBeatReceived), и для
    /// телеграфа (BPMVisualizer при спавне шара). Не зависит от порядка подписки
    /// на OnBeat, не читает изменяемое состояние — рассинхрон исключён по построению.
    /// </summary>
    public bool IsAttackBeat(int beat)
    {
        if (attackEveryBeats <= 0) return false;
        if (beat < warmupBeats) return false;
        return (beat - warmupBeats) % attackEveryBeats == 0;
    }

    /// <summary>
    /// Для отладки/UI: ближайший будущий бит атаки от заданного. -1 если враг неактивен.
    /// </summary>
    public int GetNextAttackBeatFrom(int currentBeat)
    {
        if (attackEveryBeats <= 0) return -1;
        int probe = Mathf.Max(currentBeat, warmupBeats);
        for (int i = 0; i < attackEveryBeats; i++)
        {
            if (IsAttackBeat(probe + i)) return probe + i;
        }
        return -1;
    }

    void OnBeatReceived(int beatNumber)
    {
        if (BattleManager.Instance == null || BattleManager.Instance.battleEnded) return;

        if (IsAttackBeat(beatNumber))
            Attack();
    }

    void Attack()
    {
        BattleManager.Instance.DamagePlayer(damagePerAttack);
        Debug.Log($"Enemy attacks for {damagePerAttack}!");
    }
}
