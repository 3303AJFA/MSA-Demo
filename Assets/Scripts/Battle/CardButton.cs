using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;

public class CardButton : MonoBehaviour
{
    public CardData card;
    public StringVisual stringVisual;

    [Header("Card Sprites")]
    public Sprite normalSprite;
    public Sprite armedSprite;

    [Header("Card Labels")]
    public TextMeshProUGUI labelKey;
    public TextMeshProUGUI labelRole;
    public TextMeshProUGUI labelCost;
    public Image stringLine;

    [Header("Glow")]
    [Tooltip("Дочерний Image с мягким свечением вокруг карты, включается при armed")]
    public GameObject glowObject;

    [Header("Armed Animation")]
    public float armedLiftY = 22f;

    [Header("Colors")]
    public Color normalKeyColor = new Color(0.87f, 0.82f, 0.73f);    // #DDD2BB
    public Color armedKeyColor = new Color(0.90f, 0.93f, 0.52f);     // #E6EC84
    public Color normalRoleColor = new Color(0.56f, 0.54f, 0.47f);   // #8F8A78
    public Color armedRoleColor = new Color(0.74f, 0.77f, 0.29f);    // #BCC54A
    public Color normalStringColor = new Color(0.79f, 0.64f, 0.29f); // #C9A24B
    public Color armedStringColor = new Color(0.90f, 0.93f, 0.52f);  // #E6EC84
    public Color costColor = new Color(0.79f, 0.64f, 0.29f);         // #C9A24B

    private Button button;
    private Image cardImage;
    private RectTransform rt;
    private bool isArmed;
    private float baseY;

    static readonly string[] RomanNumerals = { "", "I", "II", "III", "IV", "V" };

    void Start()
    {
        button = GetComponent<Button>();
        cardImage = GetComponent<Image>();
        rt = GetComponent<RectTransform>();
        baseY = rt.anchoredPosition.y;
        if (button != null) button.onClick.AddListener(OnClick);

        if (labelCost != null && card != null)
        {
            labelCost.text = $"{card.voltageCost:F0}%";
            labelCost.color = costColor;
        }

        if (glowObject != null)
            glowObject.SetActive(false);

        ApplyColors(false);
    }

    void Update()
    {
        if (card == null) return;
        if (Keyboard.current[card.hotkey].wasPressedThisFrame)
            OnClick();

        bool shouldBeArmed = ComboSystem.Instance != null
            && ComboSystem.Instance.IsCharging
            && ComboSystem.Instance.HasCard(card);

        if (shouldBeArmed != isArmed)
        {
            isArmed = shouldBeArmed;

            if (cardImage != null)
                cardImage.sprite = isArmed ? armedSprite : normalSprite;

            rt.DOKill();
            float targetY = isArmed ? baseY + armedLiftY : baseY;
            rt.DOAnchorPosY(targetY, 0.2f).SetEase(Ease.OutBack);

            if (glowObject != null)
                glowObject.SetActive(isArmed);

            ApplyColors(isArmed);
        }
    }

    void ApplyColors(bool armed)
    {
        if (labelKey != null)
            labelKey.color = armed ? armedKeyColor : normalKeyColor;
        if (labelRole != null)
            labelRole.color = armed ? armedRoleColor : normalRoleColor;
        if (stringLine != null)
            stringLine.color = armed ? armedStringColor : normalStringColor;
    }

    void OnClick()
    {
        ComboSystem.Instance.OnCardPressed(card);
        stringVisual?.PlayHit();
    }
}
