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
    Echo,
    Poison
}

[CreateAssetMenu(fileName = "NewCard", menuName = "MSA/Card")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardName;
    public Key hotkey;

    [Tooltip("Кулдаун между нажатиями (в долях такта). Если педаль задаёт свой — берётся он. Предметы могут переопределять через ComboSystem.SetCooldownOverride")]
    public float cooldownBeats = 0.5f;

    [Header("Equipped Pedal (new system)")]
    [Tooltip("Если назначена — её Execute вызывается вместо устаревшего switch ниже.")]
    public PedalEffect equippedPedal;

    [Header("Legacy fallback (deprecated)")]
    [Tooltip("Используется только если equippedPedal == null. Старая модель — удалить после миграции всех карт на педали.")]
    [Range(0f, 100f)] public float voltageCost = 20f;
    public int damage = 0;
    public CardEffect effect;
    public float effectValue = 0f;
    public float effectDuration = 0f;

    public float GetVoltageCost()
    {
        if (equippedPedal != null) return equippedPedal.GetVoltageCost();
        return voltageCost;
    }

    public float GetCooldownBeats()
    {
        if (equippedPedal != null)
        {
            float pedalCd = equippedPedal.GetCooldownBeats();
            if (pedalCd > 0f) return pedalCd;
        }
        return cooldownBeats;
    }
}