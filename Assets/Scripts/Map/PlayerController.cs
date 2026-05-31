using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [Header("Movement")]
    [Tooltip("Скорость движения (units/sec)")]
    public float moveSpeed = 4f;

    [Tooltip("Скорость поворота персонажа к направлению движения (выше — резче)")]
    public float turnSpeed = 12f;

    [Header("Gravity")]
    [Tooltip("Ускорение свободного падения для CharacterController")]
    public float gravity = -9.81f;

    [Tooltip("Камера, относительно которой считается движение. Если пусто — берётся Camera.main.")]
    public Transform cameraReference;

    private CharacterController controller;
    private Vector3 velocity;

    void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector2 input = ReadMoveInput();
        Vector3 move = CameraRelativeMove(input);

        // Гравитация — простая, без прыжков. CharacterController.isGrounded работает с коллайдером пола.
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        Vector3 horizontal = move * moveSpeed;
        controller.Move((horizontal + Vector3.up * velocity.y) * Time.deltaTime);

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    Vector2 ReadMoveInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        Vector2 v = Vector2.zero;
        if (kb.wKey.isPressed) v.y += 1f;
        if (kb.sKey.isPressed) v.y -= 1f;
        if (kb.dKey.isPressed) v.x += 1f;
        if (kb.aKey.isPressed) v.x -= 1f;
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    Vector3 CameraRelativeMove(Vector2 input)
    {
        Transform camT = cameraReference != null ? cameraReference : (Camera.main != null ? Camera.main.transform : null);
        if (camT == null)
            return new Vector3(input.x, 0f, input.y);

        Vector3 fwd = camT.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward; else fwd.Normalize();

        Vector3 right = camT.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right; else right.Normalize();

        return fwd * input.y + right * input.x;
    }
}
