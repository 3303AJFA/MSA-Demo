using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BeatCircle : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private int targetBeat;
    private bool isAttack;

    [Header("Tint (заглушки до финального арта)")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color attackColor = new Color(1f, 0.3f, 0.3f);

    [Header("Preview state (шар у края, ждёт лёта)")]
    [SerializeField] private float previewScale = 0.55f;
    [SerializeField] private float previewAlpha = 0.45f;

    [Header("Arrival flash (вражеский шар)")]
    [SerializeField] private float arrivalPunchScale = 0.4f;
    [SerializeField] private float arrivalPunchDuration = 0.18f;

    [Tooltip("Image для тинта. Если пусто — берётся через GetComponent в Init.")]
    [SerializeField] private Image image;

    private bool arrivalFired;

    public void Init(Vector3 start, Vector3 end, int targetBeat)
    {
        startPos = start;
        endPos = end;
        this.targetBeat = targetBeat;
        transform.position = startPos;
        if (image == null) image = GetComponent<Image>();
        arrivalFired = false;
        ApplyBaseColor();
    }

    public void SetAttackMode(bool attack)
    {
        isAttack = attack;
        ApplyBaseColor();
    }

    void ApplyBaseColor()
    {
        if (image == null) return;
        // Меняем только RGB, alpha не трогаем — её ведёт SetAlpha по фазе (предпросмотр / полёт).
        // Так attack-флаг приоритетнее фазы: красный остаётся красным и в предпросмотре, и в полёте.
        Color target = isAttack ? attackColor : normalColor;
        float currentAlpha = image.color.a;
        image.color = new Color(target.r, target.g, target.b, currentAlpha);
    }

    void Update()
    {
        if (arrivalFired) return; // тушим после долёта, ждём окончания твина
        if (BeatManager.Instance == null) { Destroy(gameObject); return; }

        float songPos = BeatManager.Instance.SongPositionInBeats;
        float beatsUntil = targetBeat - songPos;

        if (beatsUntil > 1f)
        {
            // Предпросмотр у края: ждём свой бит лёта. Маленький + полупрозрачный.
            transform.position = startPos;
            transform.localScale = Vector3.one * previewScale;
            SetAlpha(previewAlpha);
        }
        else if (beatsUntil >= 0f)
        {
            // Лёт: 1 бит от края к центру.
            float t = 1f - beatsUntil;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, t);
            SetAlpha(1f);
        }
        else
        {
            // Долёт: фиксируем в центре, при атаке — вспышка, потом уничтожаем.
            arrivalFired = true;
            transform.position = endPos;
            transform.localScale = Vector3.one;

            if (isAttack)
            {
                transform.DOPunchScale(Vector3.one * arrivalPunchScale, arrivalPunchDuration, 4, 0.5f)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(() => { if (this != null) Destroy(gameObject); });
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void SetAlpha(float a)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = a;
        image.color = c;
    }
}
