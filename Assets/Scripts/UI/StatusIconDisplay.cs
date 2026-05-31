using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Иконки статусов (яд, bleed, в будущем — другие) над 3D-моделями бойцов.
/// Привязка координат — те же Transform врага/игрока и Canvas, что у DamageNumberSpawner.
/// Иконки укладываются горизонтальным стопом над целью.
/// </summary>
public class StatusIconDisplay : MonoBehaviour
{
    public static StatusIconDisplay Instance;

    [Header("World targets (3D)")]
    public Transform enemyTarget;
    public Transform playerTarget;
    public Camera worldCamera;
    public RectTransform canvasRect;

    [Tooltip("Смещение в мире по Y — над моделью. Выше DamageNumberSpawner, чтобы не наезжали.")]
    public float verticalWorldOffset = 2.5f;

    [Header("Icon prefab")]
    [Tooltip("Префаб иконки. Должен содержать Image (картинка статуса) и TextMeshProUGUI (счётчик).")]
    public GameObject iconPrefab;

    [Tooltip("Горизонтальный отступ между иконками в стопке")]
    public float iconHorizontalSpacing = 55f;

    [Header("Status sprites (заглушки до финального арта)")]
    public Sprite bleedIcon;
    public Sprite poisonIcon;

    private Dictionary<(Transform target, string id), IconInstance> active =
        new Dictionary<(Transform, string), IconInstance>();

    void Awake() => Instance = this;

    // --- Public API ---

    public void ShowBleedOnEnemy(int ticks) => Show(enemyTarget, "bleed", bleedIcon, ticks);
    public void UpdateBleedOnEnemy(int ticks) => UpdateCounter(enemyTarget, "bleed", ticks);
    public void HideBleedOnEnemy() => Hide(enemyTarget, "bleed");

    public void ShowPoisonOnEnemy(int ticks) => Show(enemyTarget, "poison", poisonIcon, ticks);
    public void UpdatePoisonOnEnemy(int ticks) => UpdateCounter(enemyTarget, "poison", ticks);
    public void HidePoisonOnEnemy() => Hide(enemyTarget, "poison");

    // (При необходимости — те же методы для playerTarget. Сейчас яд/bleed работают только по врагу.)

    // --- Implementation ---

    void Show(Transform target, string id, Sprite icon, int counter)
    {
        if (target == null || iconPrefab == null) return;

        var key = (target, id);
        if (active.TryGetValue(key, out var existing))
        {
            existing.SetSprite(icon);
            existing.SetCounter(counter);
            return;
        }

        var go = Instantiate(iconPrefab, canvasRect);
        var inst = new IconInstance(go);
        inst.SetSprite(icon);
        inst.SetCounter(counter);
        active[key] = inst;
    }

    void UpdateCounter(Transform target, string id, int counter)
    {
        if (active.TryGetValue((target, id), out var inst))
            inst.SetCounter(counter);
    }

    void Hide(Transform target, string id)
    {
        var key = (target, id);
        if (active.TryGetValue(key, out var inst))
        {
            if (inst.go != null) Destroy(inst.go);
            active.Remove(key);
        }
    }

    void LateUpdate()
    {
        // Каждый кадр пересчитываем позиции икон, чтобы они держались над целями.
        // Группируем по target, чтобы делать горизонтальный stack.
        var groupedKeys = new Dictionary<Transform, List<(Transform target, string id)>>();
        foreach (var kvp in active)
        {
            var t = kvp.Key.target;
            if (t == null) continue;
            if (!groupedKeys.TryGetValue(t, out var list))
            {
                list = new List<(Transform, string)>();
                groupedKeys[t] = list;
            }
            list.Add(kvp.Key);
        }

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null || canvasRect == null) return;

        foreach (var group in groupedKeys)
        {
            var target = group.Key;
            var keys = group.Value;

            Vector3 worldPos = target.position + Vector3.up * verticalWorldOffset;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, null, out Vector2 localCenter);

            int n = keys.Count;
            float totalWidth = (n - 1) * iconHorizontalSpacing;
            Vector2 leftStart = localCenter + Vector2.left * (totalWidth * 0.5f);
            for (int i = 0; i < n; i++)
            {
                if (active.TryGetValue(keys[i], out var inst))
                    inst.SetPosition(leftStart + Vector2.right * (i * iconHorizontalSpacing));
            }
        }
    }

    /// <summary>
    /// Принудительно сбросить все иконки (вызывается из StatusEffect.ResetAll на старте боя).
    /// </summary>
    public void HideAll()
    {
        foreach (var kvp in active)
            if (kvp.Value.go != null) Destroy(kvp.Value.go);
        active.Clear();
    }

    private class IconInstance
    {
        public GameObject go;
        private RectTransform rt;
        private Image image;
        private TextMeshProUGUI counterText;

        public IconInstance(GameObject go)
        {
            this.go = go;
            rt = (RectTransform)go.transform;
            image = go.GetComponent<Image>();
            // Если Image не на корне — ищем глубже (но не TMP)
            if (image == null) image = go.GetComponentInChildren<Image>();
            counterText = go.GetComponentInChildren<TextMeshProUGUI>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        public void SetSprite(Sprite s)
        {
            if (image != null) image.sprite = s;
        }

        public void SetCounter(int n)
        {
            if (counterText != null) counterText.text = n.ToString();
        }

        public void SetPosition(Vector2 pos)
        {
            if (rt != null) rt.anchoredPosition = pos;
        }
    }
}
