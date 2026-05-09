using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Vector3 focusPoint = Vector3.zero;

    [Header("Orbit")]
    public float distance = 8f;
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float zoomSpeed = 2f;

    [Header("Rotation")]
    public float orbitSpeed = 150f;
    public float verticalAngle = 35f;

    [Header("Инерция")]
    public float inertiaDecay = 5f; // скорость затухания (выше = быстрее останавливается)
    public float inertiaThreshold = 0.01f; // порог остановки

    private float currentAngle = 0f;
    private float angularVelocity = 0f; // текущая скорость вращения

    void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            float mouseX = Mouse.current.delta.x.ReadValue();
            angularVelocity = mouseX * orbitSpeed * Time.deltaTime;
            currentAngle += angularVelocity;
        }
        else
        {
            // Инерция — затухает со временем
            if (Mathf.Abs(angularVelocity) > inertiaThreshold)
            {
                currentAngle += angularVelocity;
                angularVelocity = Mathf.Lerp(angularVelocity, 0f, inertiaDecay * Time.deltaTime);
            }
            else
            {
                angularVelocity = 0f;
            }
        }

        // Зум колёсиком
        float scroll = Mouse.current.scroll.y.ReadValue();
        distance -= scroll * zoomSpeed * 0.01f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // Позиция камеры
        float radAngle = currentAngle * Mathf.Deg2Rad;
        float vertRad = verticalAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Sin(radAngle) * Mathf.Cos(vertRad),
            Mathf.Sin(vertRad),
            Mathf.Cos(radAngle) * Mathf.Cos(vertRad)
        ) * distance;

        transform.position = focusPoint + offset;
        transform.LookAt(focusPoint);
    }
}