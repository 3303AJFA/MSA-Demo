using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboWindowUI : MonoBehaviour
{
    public static ComboWindowUI Instance;

    [Header("References")]
    public Transform slotsContainer;   // CW_Slots
    public Image timerBar;             // CW_Timer
    public GameObject slotPrefab;      // ComboSlot
    public CanvasGroup canvasGroup;    // на ComboWindow

    private List<GameObject> activeSlots = new List<GameObject>();
    private float timerStart;
    private float timerDuration;
    private bool timerActive = false;

    void Awake() => Instance = this;

    void Start()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (timerActive)
        {
            float elapsed = Time.time - timerStart;
            float t = 1f - Mathf.Clamp01(elapsed / timerDuration);
            timerBar.fillAmount = t;
        }

        // Плавно скрываем когда нет активных слотов
        float targetAlpha = activeSlots.Count > 0 ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 4f);
    }

    public void AddCard(CardData card)
    {
        var slot = Instantiate(slotPrefab, slotsContainer);
        var label = slot.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = card.cardName;
        activeSlots.Add(slot);

        // Запускаем/обновляем таймер
        timerStart = Time.time;
        timerDuration = ComboSystem.Instance.comboWindow;
        timerActive = true;
    }

    public void ClearSlots()
    {
        foreach (var s in activeSlots) Destroy(s);
        activeSlots.Clear();
        timerActive = false;
        timerBar.fillAmount = 0f;
    }
}