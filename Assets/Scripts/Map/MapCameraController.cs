using UnityEngine;
using UnityEngine.InputSystem;

public class MapCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // Hero figure

    [Header("Distance")]
    public float distance = 10f;
    public float minDistance = 4f;
    public float maxDistance = 20f;
    public float zoomSpeed = 2f;

    [Header("Orbit")]
    public float horizontalSpeed = 200f;
    public float verticalSpeed = 120f;
    public float minPitch = 15f;      // не опуститься ниже горизонта
    public float maxPitch = 80f;      // не залезть прямо сверху

    [Header("Smoothing")]
    public float positionLerp = 8f;   // плавное следование за героем
    public float rotationLerp = 12f;

    [Header("Inertia")]
    public float inertiaDecay = 5f;

    private float yaw = 0f;
    private float pitch = 40f;
    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleOrbit();
        UpdateCameraTransform();
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.y.ReadValue();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed * 0.01f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    void HandleOrbit()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yawVelocity = delta.x * horizontalSpeed * Time.deltaTime;
            pitchVelocity = -delta.y * verticalSpeed * Time.deltaTime;

            yaw += yawVelocity;
            pitch += pitchVelocity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            // Инерция
            if (Mathf.Abs(yawVelocity) > 0.01f)
            {
                yaw += yawVelocity;
                yawVelocity = Mathf.Lerp(yawVelocity, 0f, inertiaDecay * Time.deltaTime);
            }
            if (Mathf.Abs(pitchVelocity) > 0.01f)
            {
                pitch += pitchVelocity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                pitchVelocity = Mathf.Lerp(pitchVelocity, 0f, inertiaDecay * Time.deltaTime);
            }
        }
    }

    void UpdateCameraTransform()
    {
        // Считаем целевую позицию вокруг героя
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = target.position + offset;

        // Плавное следование
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionLerp * Time.deltaTime);

        // Смотрим на героя плавно
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerp * Time.deltaTime);
    }
}