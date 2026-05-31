using UnityEngine;
using UnityEngine.UI;

public class ComboWindowUI : MonoBehaviour
{
    public static ComboWindowUI Instance;

    [Header("Canvas")]
    public CanvasGroup canvasGroup;

    [Header("Beat Rings")]
    public RectTransform innerRing;
    public RectTransform outerRing;
    public float outerStartScale = 3f;

    [Header("Ring Colors")]
    public Image outerRingImage;
    public Color chargingColor = Color.white;
    public Color perfectColor = new Color(1f, 0.85f, 0.2f, 1f);

    private bool flashedThisPulse;

    void Awake() => Instance = this;

    void Start()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (outerRing != null)
            outerRing.localScale = Vector3.one * outerStartScale;
    }

    void Update()
    {
        bool charging = ComboSystem.Instance != null && ComboSystem.Instance.IsCharging;

        if (charging)
        {
            float progress = ComboSystem.Instance.RingProgress;

            if (progress < 0.1f && flashedThisPulse)
            {
                flashedThisPulse = false;
                if (outerRingImage != null)
                    outerRingImage.color = chargingColor;
            }

            float scale = Mathf.Lerp(outerStartScale, 1f, progress);
            if (outerRing != null)
                outerRing.localScale = Vector3.one * scale;

            if (progress >= 0.9f && !flashedThisPulse)
            {
                flashedThisPulse = true;
                if (outerRingImage != null)
                    outerRingImage.color = perfectColor;
            }
        }
        else
        {
            if (outerRing != null)
                outerRing.localScale = Vector3.one * outerStartScale;
            if (outerRingImage != null)
                outerRingImage.color = chargingColor;
            flashedThisPulse = false;
        }

        if (canvasGroup != null)
        {
            float targetAlpha = charging ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 4f);
        }
    }

    // Слоты-чипы убраны по концепту (`battle_concept.html`).
    // API остаётся для совместимости с ComboSystem — это no-op.
    public void AddCard(CardData card) { }
    public void ClearSlots()
    {
        flashedThisPulse = false;
        if (outerRing != null)
            outerRing.localScale = Vector3.one * outerStartScale;
        if (outerRingImage != null)
            outerRingImage.color = chargingColor;
    }
}
