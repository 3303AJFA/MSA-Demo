using UnityEngine;
using UnityEngine.InputSystem;

public enum CardEffect
{
    Damage,
    Amplify,
    Shield,
    Bleed,
    Stun,
    Knockback,
    Echo
}

[CreateAssetMenu(fileName = "NewCard", menuName = "MSA/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    [Range(0f, 100f)]
    public float voltageCost = 20f;  // в процентах
    public int damage = 0;
    public CardEffect effect;
    public float effectValue = 0f;
    public float effectDuration = 0f;
    public Key hotkey;
}