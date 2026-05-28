using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StringVisual : MonoBehaviour
{
    [Header("Visual Elements")]
    public RectTransform stringLine;  // сама "струна" — тонкая Image
    public Image stringImage;          // её Image для смены цвета

    [Header("Anim Settings")]
    public float shakeDuration = 0.35f;
    public float shakeStrength = 5f;
    public int shakeVibrato = 25;
    public Color normalColor = Color.white;
    public Color flashColor = new Color(1f, 0.85f, 0.2f); // золотая вспышка
    public float flashInDuration = 0.05f;
    public float flashOutDuration = 0.4f;

    private Vector3 originalPos;

    void Start()
    {
        if (stringLine != null) originalPos = stringLine.localPosition;
        if (stringImage != null) stringImage.color = normalColor;
    }

    public void PlayHit()
    {
        if (stringLine == null) return;

        // Сброс если уже анимируется
        stringLine.DOKill();
        stringImage?.DOKill();
        stringLine.localPosition = originalPos;

        // Дрожание по перпендикуляру к струне
        stringLine.DOShakeAnchorPos(
            shakeDuration,
            new Vector3(shakeStrength, 0, 0),
            shakeVibrato,
            randomness: 0f,
            snapping: false,
            fadeOut: true
        ).OnComplete(() => stringLine.localPosition = originalPos);

        // Вспышка цвета
        if (stringImage != null)
        {
            stringImage.color = normalColor;
            stringImage.DOColor(flashColor, flashInDuration)
                .OnComplete(() => stringImage.DOColor(normalColor, flashOutDuration));
        }
    }
}