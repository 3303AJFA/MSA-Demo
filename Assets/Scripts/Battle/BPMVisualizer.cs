using UnityEngine;

public class BPMVisualizer : MonoBehaviour
{
    [Header("References")]
    public RectTransform leftSpawn;
    public RectTransform rightSpawn;
    public RectTransform centerPoint;
    public GameObject circlePrefab;

    [Header("Audio")]
    public AudioClip beatStrong;
    public AudioClip beatWeak;

    [Header("Queue")]
    [Tooltip("Сколько будущих битов показано у края до начала лёта. Шар спавнится за (1 + previewBeats) битов до удара.")]
    public int previewBeats = 2;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.4f;

        BeatManager.Instance.OnBeat += OnBeat;

        // Первичное заполнение очереди: спавним шары для битов [1, 2, ..., 1+previewBeats].
        // Самый ближний (бит 1) долетает первым; следующие сидят у края как предпросмотр.
        for (int beat = 1; beat <= 1 + previewBeats; beat++)
            SpawnPairForBeat(beat);
    }

    void OnBeat(int beatNumber)
    {
        bool strongBeat = (beatNumber % 4 == 1);
        var clip = strongBeat ? beatStrong : beatWeak;
        if (clip != null) audioSource.PlayOneShot(clip);

        // Самый старый шар (этот бит) прилетел в центр и уничтожен.
        // Добавляем новый в хвост очереди — это превью на (previewBeats+1) битов вперёд.
        int newBeat = beatNumber + 1 + previewBeats;
        SpawnPairForBeat(newBeat);
    }

    void SpawnPairForBeat(int beat)
    {
        // Телеграф вражеского удара через детерминированный запрос — без зависимости
        // от порядка подписки на OnBeat и без чтения изменяемого состояния.
        bool isAttack = EnemyAttack.Instance != null && EnemyAttack.Instance.IsAttackBeat(beat);
        SpawnCircle(leftSpawn.position, centerPoint.position, beat, isAttack);
        SpawnCircle(rightSpawn.position, centerPoint.position, beat, isAttack);
    }

    void SpawnCircle(Vector3 from, Vector3 to, int targetBeat, bool isAttack)
    {
        var circle = Instantiate(circlePrefab, from, Quaternion.identity, transform);
        var bc = circle.GetComponent<BeatCircle>();
        bc.Init(from, to, targetBeat);
        bc.SetAttackMode(isAttack);
    }
}
