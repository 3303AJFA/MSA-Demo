using System.Collections;
using UnityEngine;

public class StatusEffect : MonoBehaviour
{
    public static StatusEffect Instance;

    // Состояния
    public bool isStunned = false;
    public bool isBleeding = false;
    public float bleedDamage = 5f;
    public float bleedInterval = 0.5f;

    // POISON — параллельно bleed, разные числа и интервал
    public bool isPoisoned = false;
    public float poisonDamage = 3f;
    public float poisonInterval = 1f;

    // Бафф (стакающийся)
    public int amplifyStacks = 0;
    [Tooltip("База множителя при 1 стаке. x = base^stacks (2^1=x2, 2^2=x4, 2^3=x8...)")]
    public float amplifyStackBase = 2f;

    // Щит
    public int shieldHP = 0;
    [Tooltip("Сколько HP щита теряется в секунду (0 = не теряется)")]
    public float shieldDecayPerSecond = 1f;
    private float decayAccumulator;

    void Awake() => Instance = this;

    /// <summary>
    /// Чистый лист на старте боя. Сбрасывает все боевые состояния и останавливает
    /// активные корутины bleed/stun/poison. Вызывается из BattleManager.StartBattle().
    /// </summary>
    public void ResetAll()
    {
        StopAllCoroutines();
        shieldHP = 0;
        decayAccumulator = 0f;
        amplifyStacks = 0;
        isBleeding = false;
        isStunned = false;
        isPoisoned = false;
        StatusIconDisplay.Instance?.HideAll();
    }

    void Update()
    {
        if (shieldHP > 0 && shieldDecayPerSecond > 0f)
        {
            decayAccumulator += Time.deltaTime * shieldDecayPerSecond;
            int decayInt = Mathf.FloorToInt(decayAccumulator);
            if (decayInt > 0)
            {
                shieldHP = Mathf.Max(0, shieldHP - decayInt);
                decayAccumulator -= decayInt;
            }
        }
        else
        {
            decayAccumulator = 0f;
        }
    }

    // BLEED
    public void ApplyBleed(float damage, float duration)
    {
        if (isBleeding) return;
        bleedDamage = damage;
        StartCoroutine(BleedRoutine(duration));
    }

    private IEnumerator BleedRoutine(float duration)
    {
        isBleeding = true;
        int totalTicks = Mathf.CeilToInt(duration / bleedInterval);
        int remaining = totalTicks;
        StatusIconDisplay.Instance?.ShowBleedOnEnemy(remaining);

        float elapsed = 0f;
        while (elapsed < duration && remaining > 0)
        {
            yield return new WaitForSeconds(bleedInterval);
            BattleManager.Instance.DamageEnemy((int)bleedDamage);
            remaining--;
            StatusIconDisplay.Instance?.UpdateBleedOnEnemy(remaining);
            Debug.Log($"Bleed tick! Enemy HP: {BattleManager.Instance.enemyHP}, ticks left: {remaining}");
            elapsed += bleedInterval;
        }
        isBleeding = false;
        StatusIconDisplay.Instance?.HideBleedOnEnemy();
    }

    // POISON — параллельно bleed, отдельная корутина и иконка
    public void ApplyPoison(float damage, float duration)
    {
        if (isPoisoned) return;
        poisonDamage = damage;
        StartCoroutine(PoisonRoutine(duration));
    }

    private IEnumerator PoisonRoutine(float duration)
    {
        isPoisoned = true;
        int totalTicks = Mathf.CeilToInt(duration / poisonInterval);
        int remaining = totalTicks;
        StatusIconDisplay.Instance?.ShowPoisonOnEnemy(remaining);

        float elapsed = 0f;
        while (elapsed < duration && remaining > 0)
        {
            yield return new WaitForSeconds(poisonInterval);
            BattleManager.Instance.DamageEnemy((int)poisonDamage);
            remaining--;
            StatusIconDisplay.Instance?.UpdatePoisonOnEnemy(remaining);
            Debug.Log($"Poison tick! Enemy HP: {BattleManager.Instance.enemyHP}, ticks left: {remaining}");
            elapsed += poisonInterval;
        }
        isPoisoned = false;
        StatusIconDisplay.Instance?.HidePoisonOnEnemy();
    }

    // STUN
    public void ApplyStun(float duration)
    {
        if (isStunned) return;
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        Debug.Log("Enemy STUNNED!");
        yield return new WaitForSeconds(duration);
        isStunned = false;
        Debug.Log("Stun ended");
    }

    // AMPLIFY
    public void ApplyAmplify()
    {
        amplifyStacks++;
        Debug.Log($"AMPLIFIED! Stacks: {amplifyStacks} (x{Mathf.Pow(amplifyStackBase, amplifyStacks)})");
    }

    public float ConsumeAmplify()
    {
        if (amplifyStacks <= 0) return 1f;
        float mult = Mathf.Pow(amplifyStackBase, amplifyStacks);
        amplifyStacks = 0;
        return mult;
    }

    // SHIELD
    public void ApplyShield(int amount)
    {
        int cap = BattleManager.Instance != null ? BattleManager.Instance.playerMaxHP : int.MaxValue;
        shieldHP = Mathf.Min(shieldHP + amount, cap);
        Debug.Log($"Shield: {shieldHP} HP (cap {cap})");
    }

    public int AbsorbDamage(int incomingDamage)
    {
        if (shieldHP <= 0) return incomingDamage;
        int absorbed = Mathf.Min(shieldHP, incomingDamage);
        shieldHP -= absorbed;
        Debug.Log($"Shield absorbed {absorbed}! Shield left: {shieldHP}");
        return incomingDamage - absorbed;
    }
}
