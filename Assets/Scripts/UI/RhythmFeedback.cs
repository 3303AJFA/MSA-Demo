using System.Collections;
using UnityEngine;
using TMPro;

public class RhythmFeedback : MonoBehaviour
{
    public static RhythmFeedback Instance;

    public TextMeshProUGUI rhythmText;
    public float displayDuration = 0.8f;

    void Awake() => Instance = this;

    void Start()
    {
        if (rhythmText != null) rhythmText.text = "";
    }

    public void Show(string rating, Color color)
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(rating, color));
    }

    IEnumerator ShowRoutine(string rating, Color color)
    {
        rhythmText.text = rating;
        rhythmText.color = color;

        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 1.5f;
        Vector3 endScale = Vector3.one * 1.0f;

        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;

            rhythmText.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            // Затухание под конец
            Color c = rhythmText.color;
            c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.5f) * 2f));
            rhythmText.color = c;

            yield return null;
        }

        rhythmText.text = "";
    }
}