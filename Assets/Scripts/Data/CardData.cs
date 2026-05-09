using UnityEngine;
using UnityEngine.InputSystem;

public enum CardEffect
{
    Damage,      // E↓ — урон
    Amplify,     // A — следующая карта x2
    Shield,      // D — щит
    Bleed,       // G — кровотечение
    Stun,        // G — стан
    Knockback,   // B — откидывает врага
    Echo         // E↑ — повторяет последний эффект
}

[CreateAssetMenu(fileName = "NewCard", menuName = "MSA/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public int voltageCost = 1;
    public int damage = 0;
    public CardEffect effect;
    public float effectValue = 0f;  // универсальное значение эффекта
    public float effectDuration = 0f; // длительность дебаффа
    public Key hotkey;
}