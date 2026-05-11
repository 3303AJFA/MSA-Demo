using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    [Header("Combo Window")]
    public float comboWindow = 0.6f;
    public int maxComboSize = 6;

    private List<CardData> queuedCards = new List<CardData>();
    private Coroutine comboTimer;

    void Awake() => Instance = this;

    public void OnCardPressed(CardData card)
    {
        if (queuedCards.Count >= maxComboSize)
        {
            Debug.Log("Combo limit reached!");
            return;
        }

        queuedCards.Add(card);
        AudioSystem.Instance?.PlayPick();
        ComboWindowUI.Instance?.AddCard(card);
        Debug.Log($"Queued: {card.cardName}");

        if (comboTimer != null) StopCoroutine(comboTimer);
        comboTimer = StartCoroutine(ComboWindowTimer());
    }

    private IEnumerator ComboWindowTimer()
    {
        yield return new WaitForSeconds(comboWindow);
        FireCombo();
    }

    void FireCombo()
    {
        if (queuedCards.Count == 0) return;

        float totalCost = 0f;
        foreach (var card in queuedCards)
            totalCost += card.voltageCost;

        if (!BattleManager.Instance.TrySpendVoltage(totalCost))
        {
            Debug.Log("Not enough Voltage!");
            queuedCards.Clear();
            ComboWindowUI.Instance?.ClearSlots();
            return;
        }

        AudioSystem.Instance?.PlayCombo(new List<CardData>(queuedCards));

        foreach (var card in queuedCards)
            BattleManager.Instance.ExecuteCard(card);

        Debug.Log($"=== Combo fired ===");
        queuedCards.Clear();
        ComboWindowUI.Instance?.ClearSlots();
    }
}