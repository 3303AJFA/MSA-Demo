using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public int maxVoltage = 3;
    public float currentVoltage = 3;
    public float voltageRegenRate = 1f;
    public int playerHP = 100;
    public int enemyHP = 100;

    private CardData lastUsedCard; // для Echo

    void Awake() => Instance = this;

    void Update()
    {
        if (currentVoltage < maxVoltage)
        {
            currentVoltage += voltageRegenRate * Time.deltaTime;
            currentVoltage = Mathf.Min(currentVoltage, maxVoltage);
        }
    }

    public bool TrySpendVoltage(int amount)
    {
        if (currentVoltage < amount) return false;
        currentVoltage -= amount;
        return true;
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
                Debug.Log("KNOCKBACK! Enemy pushed away");
                // Анимацию добавим позже
                break;

            case CardEffect.Echo:
                if (lastUsedCard != null && lastUsedCard.effect != CardEffect.Echo)
                {
                    Debug.Log($"ECHO: repeating {lastUsedCard.cardName}");
                    ExecuteCard(lastUsedCard);
                    return; // не перезаписывать lastUsedCard
                }
                break;
        }

        lastUsedCard = card;
        Debug.Log($"Executed: {card.cardName} | Effect: {card.effect}");
    }

    public void DamageEnemy(int damage)
    {
        enemyHP -= damage;
        enemyHP = Mathf.Max(0, enemyHP);
        Debug.Log($"Enemy HP: {enemyHP}");

        if (enemyHP <= 0)
            Debug.Log("ENEMY DEFEATED!");
    }

    public void DamagePlayer(int damage)
    {
        int finalDamage = StatusEffect.Instance.AbsorbDamage(damage);
        playerHP -= finalDamage;
        playerHP = Mathf.Max(0, playerHP);
        Debug.Log($"Player HP: {playerHP}");
    }
}