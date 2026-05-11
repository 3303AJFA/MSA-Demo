using System;
using UnityEngine;

[Serializable]
public class EventChoice
{
    [TextArea(1, 2)]
    public string choiceText;

    [Header("Outcome")]
    [TextArea(2, 5)]
    public string outcomeText;
    public Sprite outcomeImage;

    [Header("Effects")]
    public int hpChange;
    public int voltageChange;
    public int coinsChange;

    [Header("Optional")]
    public bool triggersCombat;
    public bool grantsPedal;
}