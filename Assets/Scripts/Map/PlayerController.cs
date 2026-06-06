using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public enum Mode { Movement, Guitar }
    public Mode CurrentMode { get; private set; } = Mode.Movement;

    [Header("Movement Feel")]
    [Tooltip("Максимальная горизонтальная скорость (units/sec)")]
    public float maxSpeed = 5f;

    [Tooltip("Ускорение при нажатой кнопке (units/sec²). Выше = быстрее набирает скорость.")]
    public float acceleration = 40f;

    [Tooltip("Торможение при отпущенной кнопке (units/sec²). Меньше acceleration — даёт «вес».")]
    public float deceleration = 25f;

    [Tooltip("Торможение при резкой смене направления (dot ввода и текущей скорости < 0) — «занос на развороте».")]
    public float turnDeceleration = 30f;

    [Header("Turn (Rotation) Momentum")]
    public float turnSpeedMoving = 8f;
    public float turnSpeedStationary = 14f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    [Header("Camera")]
    [Tooltip("Камера, относительно которой считается WASD. Если пусто — Camera.main.")]
    public Transform cameraReference;

    [Header("Animation (code-driven)")]
    [SerializeField] private Animator animator;

    [Tooltip("Float-параметр блендинга idle/walk/run. По умолчанию \"Speed\".")]
    [SerializeField] private string speedParamName = "Speed";

    [Tooltip("Bool-параметр для гитарной позы (опционально). Animator может реагировать переходом на «играет на гитаре». Если параметра нет, Animator просто игнорирует.")]
    [SerializeField] private string playingParamName = "Playing";

    [Header("Guitar Mode")]
    [Tooltip("Удерживать эту клавишу — встать и играть QWEASD как струны.")]
    [SerializeField] private Key guitarHoldKey = Key.LeftAlt;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;

    // Кэш: есть ли в текущем Animator Controller эти параметры. Проверяется один раз
    // на старте, чтобы не дёргать SetFloat/SetBool для отсутствующих параметров
    // (Unity иначе спамит "Parameter X does not exist." варнингами каждый кадр).
    private bool hasSpeedParam;
    private bool hasPlayingParam;

    void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        RefreshAnimatorParamCache();
    }

    void RefreshAnimatorParamCache()
    {
        hasSpeedParam = false;
        hasPlayingParam = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (var p in animator.parameters)
        {
            if (p.name == speedParamName && p.type == AnimatorControllerParameterType.Float)
                hasSpeedParam = true;
            else if (p.name == playingParamName && p.type == AnimatorControllerParameterType.Bool)
                hasPlayingParam = true;
        }
    }

    void Update()
    {
        UpdateModeFromInput();

        if (CurrentMode == Mode.Movement)
            TickMovement();
        else
            TickGuitar();

        UpdateGravity();
        ApplyMovement();
        UpdateAnimation();
    }

    void UpdateModeFromInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool guitarHeld = kb[guitarHoldKey].isPressed;
        Mode wanted = guitarHeld ? Mode.Guitar : Mode.Movement;

        if (wanted == CurrentMode) return;

        CurrentMode = wanted;

        if (CurrentMode == Mode.Guitar)
        {
            // Вход в гитарный режим: гасим скорость, чтобы персонаж встал.
            horizontalVelocity = Vector3.zero;
        }
    }

    void TickMovement()
    {
        Vector2 input = ReadMoveInput();
        Vector3 inputDir = CameraRelativeMove(input);

        UpdateHorizontalVelocity(inputDir);
        UpdateRotation();
    }

    void TickGuitar()
    {
        // Движение заблокировано — даже если игрок жмёт WASD, ничего не происходит.
        horizontalVelocity = Vector3.zero;

        var kb = Keyboard.current;
        if (kb == null) return;

        // Q/W/E/A/S/D = 6 струн. Маппинг тот же, что в бою (CardData hotkeys).
        if (kb.qKey.wasPressedThisFrame) PlayString(0);
        if (kb.wKey.wasPressedThisFrame) PlayString(1);
        if (kb.eKey.wasPressedThisFrame) PlayString(2);
        if (kb.aKey.wasPressedThisFrame) PlayString(3);
        if (kb.sKey.wasPressedThisFrame) PlayString(4);
        if (kb.dKey.wasPressedThisFrame) PlayString(5);
    }

    void PlayString(int index)
    {
        if (AudioSystem.Instance != null)
            AudioSystem.Instance.PlayNote(index);
        // Если AudioSystem нет в сцене — тихо игнорируем (логировать каждый раз шумно).
    }

    void UpdateHorizontalVelocity(Vector3 inputDir)
    {
        Vector3 targetVel = inputDir * maxSpeed;
        bool hasInput = inputDir.sqrMagnitude > 0.001f;

        float rate;
        if (!hasInput)
        {
            rate = deceleration;
        }
        else
        {
            Vector3 currentDir = horizontalVelocity.sqrMagnitude > 0.001f
                ? horizontalVelocity.normalized
                : Vector3.zero;
            float dot = Vector3.Dot(currentDir, inputDir);

            if (horizontalVelocity.magnitude > 0.5f && dot < 0f)
                rate = turnDeceleration;
            else
                rate = acceleration;
        }

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVel, rate * Time.deltaTime);
    }

    void UpdateRotation()
    {
        if (horizontalVelocity.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(horizontalVelocity, Vector3.up);
        float speedNorm = Mathf.Clamp01(horizontalVelocity.magnitude / Mathf.Max(maxSpeed, 0.001f));
        float turnSpeed = Mathf.Lerp(turnSpeedStationary, turnSpeedMoving, speedNorm);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    void UpdateGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
    }

    void ApplyMovement()
    {
        Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    void UpdateAnimation()
    {
        if (animator == null) return;

        if (hasSpeedParam)
        {
            float speedNorm = horizontalVelocity.magnitude / Mathf.Max(maxSpeed, 0.001f);
            animator.SetFloat(speedParamName, speedNorm);
        }

        if (hasPlayingParam)
        {
            animator.SetBool(playingParamName, CurrentMode == Mode.Guitar);
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
        Transform camT = cameraReference != null
            ? cameraReference
            : (Camera.main != null ? Camera.main.transform : null);
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
