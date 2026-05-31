using UnityEngine;
using UnityEngine.InputSystem;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Iso Angles")]
    [Tooltip("Угол наклона камеры вниз (изометрический наклон). Типично 30-45°.")]
    [Range(15f, 75f)] public float pitch = 40f;

    [Tooltip("Начальный yaw (поворот вокруг персонажа)")]
    public float initialYaw = 45f;

    [Tooltip("Шаг поворота при нажатии Q/E (градусов)")]
    public float rotateStep = 90f;

    [Header("Distance")]
    public float distance = 10f;

    [Header("Smoothing")]
    [Tooltip("Скорость доводки поворота к целевому yaw")]
    public float rotateLerpSpeed = 8f;

    [Tooltip("Скорость следования камеры за персонажем")]
    public float followSpeed = 10f;

    private float currentYaw;
    private float targetYaw;

    void Start()
    {
        currentYaw = initialYaw;
        targetYaw = initialYaw;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.qKey.wasPressedThisFrame) targetYaw -= rotateStep;
            if (kb.eKey.wasPressedThisFrame) targetYaw += rotateStep;
        }
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotateLerpSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion rot = Quaternion.Euler(pitch, currentYaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, -distance);
        Vector3 desiredPos = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        Vector3 lookDir = (target.position - transform.position);
        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), followSpeed * Time.deltaTime);
    }
}
