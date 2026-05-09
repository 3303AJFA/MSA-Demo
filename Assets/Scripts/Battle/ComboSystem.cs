using System.Collections.Generic;
using UnityEngine;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    private List<CardData> queuedCards = new List<CardData>();
    private HashSet<CardData> heldCards = new HashSet<CardData>();

    void Awake() => Instance = this;

    public void OnCardPressed(CardData card)
    {
        if (heldCards.Contains(card)) return;
        heldCards.Add(card);
        queuedCards.Add(card);
        AudioSystem.Instance?.PlayPick();
    }

    public void OnCardReleased(CardData card)
    {
        if (!heldCards.Contains(card)) return;
        heldCards.Remove(card);
        if (heldCards.Count == 0)
            FireCombo();
    }

    void FireCombo()
    {
        int totalCost = 0;
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

        queuedCards.Clear();
    }
}