using UnityEngine;
using UnityEngine.UI;

public class ChargeIndicator : MonoBehaviour
{
    [Header("References")]
    public Image fillImage;

    [Header("Colors")]
    public Color earlyColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    public Color readyColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color perfectColor = new Color(0.4f, 1f, 0.4f, 1f);

    void Start()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
    }

    void Update()
    {
        if (ComboSystem.Instance == null || fillImage == null) return;

        bool charging = ComboSystem.Instance.IsCharging;
        fillImage.enabled = charging;

        if (!charging) return;

        float progress = ComboSystem.Instance.GetChargeProgress();
        fillImage.fillAmount = progress;

        // Цвет: серый → жёлтый → зелёный возле Perfect
        if (progress > 0.92f)
            fillImage.color = perfectColor;
        else if (progress > 0.75f)
            fillImage.color = readyColor;
        else
            fillImage.color = Color.Lerp(earlyColor, readyColor, progress);
    }
}