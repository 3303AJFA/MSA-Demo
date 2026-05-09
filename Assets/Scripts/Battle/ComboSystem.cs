using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    [Header("Combo Window")]
    public float comboWindow = 0.6f; // сколько ждать следующую ноту

    private List<CardData> queuedCards = new List<CardData>();
    private Coroutine comboTimer;

    void Awake() => Instance = this;

    public void OnCardPressed(CardData card)
    {
        queuedCards.Add(card);
        AudioSystem.Instance?.PlayPick();
        Debug.Log($"Queued: {card.cardName} (in combo: {queuedCards.Count})");

        // Сбрасываем таймер
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
            return;
        }

        AudioSystem.Instance?.PlayCombo(new List<CardData>(queuedCards));

        foreach (var card in queuedCards)
            BattleManager.Instance.ExecuteCard(card);

        Debug.Log($"=== Combo fired with {queuedCards.Count} cards ===");
        queuedCards.Clear();
    }
}