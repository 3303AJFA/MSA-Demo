using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance;

    [Header("References")]
    public DamageNumber prefab;
    public RectTransform canvasRect;
    public Camera worldCamera;

    [Header("Targets (3D world objects)")]
    public Transform enemyTarget;
    public Transform playerTarget;

    [Tooltip("Смещение в мире по Y, чтобы цифра появлялась НАД моделью, а не из центра")]
    public float verticalWorldOffset = 1.5f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color perfectColor = new Color(1f, 0.85f, 0.2f);
    public Color goodColor = new Color(0.7f, 0.9f, 1f);
    public Color playerDamageColor = new Color(1f, 0.3f, 0.3f);

    void Awake() => Instance = this;

    public void ShowEnemyDamage(int damage, float rhythmBonus)
    {
        Color color = normalColor;
        if (rhythmBonus >= 1.5f) color = perfectColor;
        else if (rhythmBonus >= 1.25f) color = goodColor;

        Spawn(enemyTarget, damage, color);
    }

    public void ShowPlayerDamage(int damage) => Spawn(playerTarget, damage, playerDamageColor);

    void Spawn(Transform target, int damage, Color color)
    {
        if (prefab == null || target == null || canvasRect == null) return;

        Vector3 worldPos = target.position + Vector3.up * verticalWorldOffset;
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPoint, null, out Vector2 localPoint);

        var instance = Instantiate(prefab, canvasRect);
        var rt = (RectTransform)instance.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPoint;
        rt.localScale = Vector3.one;
        instance.Play(damage, color);
    }
}
