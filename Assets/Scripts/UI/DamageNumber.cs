using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageNumber : MonoBehaviour
{
    public TextMeshProUGUI text;
    public CanvasGroup canvasGroup;

    [Header("Animation")]
    public float floatDistance = 80f;
    public float duration = 0.9f;
    public float horizontalJitter = 30f;
    public float startScale = 0.5f;
    public float punchScale = 1.2f;

    public void Play(int damage, Color color)
    {
        if (text != null)
        {
            text.text = damage.ToString();
            text.color = color;
        }

        var rt = (RectTransform)transform;
        Vector2 start = rt.anchoredPosition;
        Vector2 offset = new Vector2(Random.Range(-horizontalJitter, horizontalJitter), floatDistance);

        rt.localScale = Vector3.one * startScale;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        var seq = DOTween.Sequence();
        seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.Append(rt.DOScale(punchScale, 0.15f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(1f, 0.1f));
        seq.Join(rt.DOAnchorPos(start + offset, duration).SetEase(Ease.OutCubic));
        if (canvasGroup != null)
            seq.Insert(duration * 0.55f, canvasGroup.DOFade(0f, duration * 0.45f));
        seq.OnComplete(() =>
        {
            if (this != null && gameObject != null)
                Destroy(gameObject);
        });
    }
}
