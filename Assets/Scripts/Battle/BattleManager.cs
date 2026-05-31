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
    public int playerMaxHP = 100;
    public int enemyHP = 100;
    public int enemyMaxHP = 100;

    [Header("Battle State")]
    public bool battleEnded = false;

    [HideInInspector] public float lastRhythmBonus = 1.0f;

    private CardData lastUsedCard;

    void Awake() => Instance = this;

    void Start() => StartBattle();

    /// <summary>
    /// Единая точка инициализации боя. Чистит всё боевое состояние независимо от того,
    /// откуда взялись грязные значения (грязная сцена, отключённый Domain Reload, кеши синглтонов).
    /// Вызывается из Start() при загрузке BattleScene, или явно при «рестарте боя без перезагрузки сцены».
    /// </summary>
    public void StartBattle()
    {
        // HP / Voltage / флаги
        playerHP = playerMaxHP;
        enemyHP = enemyMaxHP;
        currentVoltage = maxVoltage;
        battleEnded = false;
        lastRhythmBonus = 1.0f;
        lastUsedCard = null;

        // Делегируем чистку соседним системам
        StatusEffect.Instance?.ResetAll();
        ComboSystem.Instance?.ResetForBattle();

        Debug.Log("=== BATTLE START — state cleared ===");
    }

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

        if (card.equippedPedal != null)
        {
            var ctx = new CombatContext
            {
                sourceCard = card,
                battle = this,
                status = status,
                rhythmBonus = lastRhythmBonus,
                amplifyMult = amplify
            };
            card.equippedPedal.Execute(ctx);
            lastUsedCard = card;
            Debug.Log($"Executed pedal: {card.equippedPedal.pedalName} on {card.cardName}");
            return;
        }

        switch (card.effect)
        {
            case CardEffect.Damage:
                int dmg = Mathf.RoundToInt(card.damage * amplify * lastRhythmBonus);
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
            case CardEffect.Poison:
                status.ApplyPoison(card.effectValue, card.effectDuration);
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

    public void DamageEnemy(int damage)
    {
        if (battleEnded) return;

        enemyHP -= damage;
        enemyHP = Mathf.Max(0, enemyHP);
        Debug.Log($"Enemy HP: {enemyHP}");

        DamageNumberSpawner.Instance?.ShowEnemyDamage(damage, lastRhythmBonus);

        if (enemyHP <= 0)
        {
            Victory();
        }
    }

    void Victory()
    {
        battleEnded = true;
        Debug.Log("=== VICTORY! ===");

        // Стрик/состояние не сбрасываем здесь — следующий бой пройдёт через StartBattle().
        if (BattleEndUI.Instance != null)
            BattleEndUI.Instance.ShowVictory();
    }

    public void DamagePlayer(int damage)
    {
        if (battleEnded) return;

        int finalDamage = StatusEffect.Instance.AbsorbDamage(damage);
        playerHP -= finalDamage;
        playerHP = Mathf.Max(0, playerHP);
        Debug.Log($"Player HP: {playerHP}");

        if (finalDamage > 0)
        {
            DamageNumberSpawner.Instance?.ShowPlayerDamage(finalDamage);
            if (ComboSystem.Instance != null)
                ComboSystem.Instance.AddStreak(-ComboSystem.Instance.streakProfile.damageLoss);
        }

        if (playerHP <= 0)
            Defeat();
    }

    void Defeat()
    {
        battleEnded = true;
        Debug.Log("=== DEFEAT ===");

        // Стрик/состояние не сбрасываем здесь — следующий бой пройдёт через StartBattle().
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.ReturnToMap();
    }
}