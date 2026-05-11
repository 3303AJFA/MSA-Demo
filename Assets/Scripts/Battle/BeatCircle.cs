using UnityEngine;
using UnityEngine.UI;

public class BeatCircle : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float duration;
    private float elapsed = 0f;

    public void Init(Vector3 start, Vector3 end, float dur)
    {
        startPos = start;
        endPos = end;
        duration = dur;
        transform.position = startPos;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(startPos, endPos, t);

        // Лёгкая анимация — растём по мере приближения
        float scale = Mathf.Lerp(0.7f, 1.2f, t);
        transform.localScale = Vector3.one * scale;

        if (t >= 1f)
            Destroy(gameObject);
    }
}