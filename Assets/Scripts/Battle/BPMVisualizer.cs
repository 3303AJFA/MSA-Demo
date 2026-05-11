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

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.4f;

        BeatManager.Instance.OnBeat += OnBeat;

        // Спавним первую пару — она долетит на первый удар
        SpawnPair();
    }

    void OnBeat(int beatNumber)
    {
        // Кружки прилетели в центр прямо сейчас
        // Звук удара
        bool strongBeat = (beatNumber % 4 == 1);
        var clip = strongBeat ? beatStrong : beatWeak;
        if (clip != null) audioSource.PlayOneShot(clip);

        // Спавним новую пару на следующий удар
        SpawnPair();
    }

    void SpawnPair()
    {
        float duration = BeatManager.Instance.SecondsPerBeat;

        SpawnCircle(leftSpawn.position, centerPoint.position, duration);
        SpawnCircle(rightSpawn.position, centerPoint.position, duration);
    }

    void SpawnCircle(Vector3 from, Vector3 to, float duration)
    {
        var circle = Instantiate(circlePrefab, from, Quaternion.identity, transform);
        var bc = circle.GetComponent<BeatCircle>();
        bc.Init(from, to, duration);
    }
}