using System.Collections;
using UnityEngine;

public class HeroFigure : MonoBehaviour
{
    public static HeroFigure Instance;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [HideInInspector] public PointOfInterest currentPOI;

    void Awake() => Instance = this;

    public void MoveTo(PointOfInterest target, System.Action onArrived)
    {
        StartCoroutine(MoveRoutine(target, onArrived));
    }

    IEnumerator MoveRoutine(PointOfInterest target, System.Action onArrived)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = target.transform.position + Vector3.up * 0.5f;
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // Лёгкий подпрыг во время движения
            float bounce = Mathf.Sin(t * Mathf.PI) * 0.3f;
            transform.position += Vector3.up * bounce;

            yield return null;
        }

        transform.position = endPos;
        currentPOI = target;
        onArrived?.Invoke();
    }
}