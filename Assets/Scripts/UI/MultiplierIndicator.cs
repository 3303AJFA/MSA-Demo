using UnityEngine;
using TMPro;
using DG.Tweening;

public class MultiplierIndicator : MonoBehaviour
{
    public static MultiplierIndicator Instance;

    [Header("References")]
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;

    [Header("Positioning")]
    [Tooltip("Смещение по Y от центра карты (вверх)")]
    public float yOffset = 130f;

    [Header("Animation")]
    public float moveDuration = 0.25f;
    public float showDuration = 0.2f;
    public float fadeDuration = 0.15f;

    private RectTransform rt;
    private int currentMultiplier;
    private bool claimed;

    void Awake()
    {
        Instance = this;
        rt = (RectTransform)transform;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void OnCardPressed(CardData card, RectTransform cardRt)
    {
        if (card == null || cardRt == null) return;

        bool isAmplify = card.effect == CardEffect.Amplify;

        if (isAmplify)
        {
            if (claimed)
            {
                currentMultiplier = 2;
                claimed = false;
                ShowAt(cardRt);
            }
            else if (currentMultiplier == 0)
            {
                currentMultiplier = 2;
                ShowAt(cardRt);
            }
            else
            {
                currentMultiplier *= 2;
                MoveToAndBump(cardRt);
            }
        }
        else
        {
            if (currentMultiplier > 0 && !claimed)
            {
                claimed = true;
                MoveTo(cardRt);
            }
        }
    }

    public void OnComboFired()
    {
        currentMultiplier = 0;
        claimed = false;
        Hide();
    }

    Vector2 TargetPos(RectTransform cardRt) =>
        cardRt.anchoredPosition + new Vector2(0, yOffset);

    void ShowAt(RectTransform cardRt)
    {
        rt.SetParent(cardRt.parent, false);
        rt.anchoredPosition = TargetPos(cardRt);
        UpdateText();

        rt.DOKill();
        canvasGroup?.DOKill();

        rt.localScale = Vector3.one * 0.5f;
        rt.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack);
        if (canvasGroup != null) canvasGroup.DOFade(1f, fadeDuration);
    }

    void MoveTo(RectTransform cardRt)
    {
        rt.SetParent(cardRt.parent, false);
        rt.DOKill();
        rt.DOAnchorPos(TargetPos(cardRt), moveDuration).SetEase(Ease.OutCubic);
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration);
        }
    }

    void MoveToAndBump(RectTransform cardRt)
    {
        rt.SetParent(cardRt.parent, false);
        rt.DOKill();
        UpdateText();
        rt.DOAnchorPos(TargetPos(cardRt), moveDuration).SetEase(Ease.OutCubic);
        rt.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f);
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration);
        }
    }

    void Hide()
    {
        rt.DOKill();
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration);
        }
    }

    void UpdateText()
    {
        if (text != null) text.text = $"x{currentMultiplier}";
    }
}
