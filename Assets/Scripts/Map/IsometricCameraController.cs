using UnityEngine;
using UnityEngine.InputSystem;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Iso Angles")]
    [Tooltip("Угол наклона камеры вниз (изометрический). Фиксированный, не меняется в плеймоде.")]
    [Range(15f, 75f)] public float pitch = 40f;

    [Tooltip("Начальный yaw — поворот вокруг персонажа на старте")]
    public float initialYaw = 45f;

    [Header("Distance")]
    public float distance = 10f;

    [Header("Rotation (Q/E hold)")]
    [Tooltip("Угловая скорость поворота при ЗАЖАТИИ Q/E (градусов/сек)")]
    public float rotateSpeed = 120f;

    [Tooltip("Мягкость старта/остановки вращения. 0 = мгновенно дёрнулось. Выше = плавнее старт/затухание.")]
    [Range(0f, 20f)] public float rotationDamping = 6f;

    [Header("Follow")]
    [Tooltip("Скорость доводки позиции камеры за target (Lerp). Выше = жёстче следует.")]
    public float followSpeed = 10f;

    private float currentYaw;
    private float angularVelocity;

    void Start()
    {
        currentYaw = initialYaw;
        angularVelocity = 0f;
    }

    void Update()
    {
        UpdateYawFromInput();
    }

    void UpdateYawFromInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float targetAngularVel = 0f;
        if (kb.qKey.isPressed) targetAngularVel -= rotateSpeed;
        if (kb.eKey.isPressed) targetAngularVel += rotateSpeed;

        // Демпфирование: angularVelocity плавно стремится к target.
        // При rotationDamping = 0 — мгновенный набор/останов.
        if (rotationDamping > 0f)
        {
            float t = 1f - Mathf.Exp(-rotationDamping * Time.deltaTime);
            angularVelocity = Mathf.Lerp(angularVelocity, targetAngularVel, t);
        }
        else
        {
            angularVelocity = targetAngularVel;
        }

        currentYaw += angularVelocity * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion rot = Quaternion.Euler(pitch, currentYaw, 0f);
        Vector3 offset = rot * new Vector3(0f, 0f, -distance);
        Vector3 desiredPos = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        Vector3 lookDir = target.position - transform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), followSpeed * Time.deltaTime);
    }
}
