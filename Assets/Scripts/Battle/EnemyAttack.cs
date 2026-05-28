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

    private int beatCounter;
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

    void OnBeatReceived(int beatNumber)
    {
        if (BattleManager.Instance == null || BattleManager.Instance.battleEnded) return;

        beatCounter++;
        if (beatCounter < warmupBeats) return;

        if ((beatCounter - warmupBeats) % attackEveryBeats == 0)
            Attack();
    }

    void Attack()
    {
        BattleManager.Instance.DamagePlayer(damagePerAttack);
        Debug.Log($"Enemy attacks for {damagePerAttack}!");
    }
}
