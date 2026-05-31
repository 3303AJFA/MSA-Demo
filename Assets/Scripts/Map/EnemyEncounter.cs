using UnityEngine;

/// <summary>
/// Триггер-зона вокруг врага. Когда PlayerController входит в радиус — запускает бой
/// через SceneFlow.GoToBattle. Объект-враг визуально — капсула-заглушка.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class EnemyEncounter : MonoBehaviour
{
    [Tooltip("Радиус триггера. Меняется в инспекторе или прямо на SphereCollider.")]
    public float triggerRadius = 1.5f;

    [Tooltip("ID энкаунтера — для будущего (выбор врага в бою). Пока не используется.")]
    public int encounterID = 0;

    private SphereCollider trigger;
    private bool fired;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = triggerRadius;
    }

    void OnValidate()
    {
        var sc = GetComponent<SphereCollider>();
        if (sc != null)
        {
            sc.isTrigger = true;
            sc.radius = triggerRadius;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (other.GetComponent<PlayerController>() == null) return;

        fired = true;
        Debug.Log($"Enemy encounter {encounterID} triggered — entering battle.");

        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToBattle(encounterID);
        else
            Debug.LogWarning("SceneFlow not found! Make sure SceneFlow exists in scene.");
    }
}
