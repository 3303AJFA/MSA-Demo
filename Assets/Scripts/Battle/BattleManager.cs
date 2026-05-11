using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Voltage (in %)")]
    public float maxVoltage = 100f;
    public float currentVoltage = 100f;
    public float voltageRegenRate = 15f; // % в секунду

    [Header("HP")]
    public int playerHP = 100;
    public int enemyHP = 100;

    private CardData lastUsedCard;

    void Awake() => Instance = this;

    void Update()
    {
        if (currentVoltage < maxVoltage)
        {
            currentVoltage += voltageRegenRate * Time.deltaTime;
            currentVoltage = Mathf.Min(currentVoltage, maxVoltage);
        }
    }

    public bool TrySpendVoltage(float amount)
    {
        if (currentVoltage < amount) return false;
        currentVoltage -= amount;
        return true;
    }

    public void AddVoltageBonus(float amount)
    {
        currentVoltage = Mathf.Min(currentVoltage + amount, maxVoltage);
        Debug.Log($"+{amount}% Voltage bonus!");
    }

    public void ExecuteCard(CardData card)
    {
        var status = StatusEffect.Instance;
        float amplify = status.ConsumeAmplify();

        switch (card.effect)
        {
            case CardEffect.Damage:
                int dmg = Mathf.RoundToInt(card.damage * amplify);
                DamageEnemy(dmg);
                break;
            case CardEffect.Amplify:
                status.ApplyAmplify();
                break;
            case CardEffect.Shield:
                status.ApplyShield((int)card.effectValue);
                break;
            case CardEffect.Bleed:
                status.ApplyBleed(card.effectValue, card.effectDuration);
                break;
            case CardEffect.Stun:
                status.ApplyStun(card.effectDuration);
                break;
            case CardEffect.Knockback:
                Debug.Log("KNOCKBACK!");
                break;
            case CardEffect.Echo:
                if (lastUsedCard != null && lastUsedCard.effect != CardEffect.Echo)
                {
                    Debug.Log($"ECHO: {lastUsedCard.cardName}");
                    ExecuteCard(lastUsedCard);
                    return;
                }
                break;
        }

        lastUsedCard = card;
        Debug.Log($"Executed: {card.cardName}");
    }

[Header("Battle State")]
public bool battleEnded = false;

public void DamageEnemy(int damage)
{
    if (battleEnded) return;

    enemyHP -= damage;
    enemyHP = Mathf.Max(0, enemyHP);
    Debug.Log($"Enemy HP: {enemyHP}");

    if (enemyHP <= 0)
    {
        Victory();
    }
}

void Victory()
{
    battleEnded = true;
    Debug.Log("=== VICTORY! ===");

    // Показываем экран победы
    if (BattleEndUI.Instance != null)
        BattleEndUI.Instance.ShowVictory();
}

    public void DamagePlayer(int damage)
    {
        int finalDamage = StatusEffect.Instance.AbsorbDamage(damage);
        playerHP -= finalDamage;
        playerHP = Mathf.Max(0, playerHP);
        Debug.Log($"Player HP: {playerHP}");
    }
}