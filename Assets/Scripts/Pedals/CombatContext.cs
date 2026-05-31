using UnityEngine;

public class CombatContext
{
    public CardData sourceCard;
    public BattleManager battle;
    public StatusEffect status;

    public float rhythmBonus = 1f;
    public float amplifyMult = 1f;

    public int ScaleDamage(float baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * rhythmBonus * amplifyMult);
    }
}
