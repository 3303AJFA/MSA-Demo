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

    // Бафф
    public bool isAmplified = false;
    public float amplifyMultiplier = 2f;

    // Щит
    public int shieldHP = 0;

    void Awake() => Instance = this;

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
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(bleedInterval);
            BattleManager.Instance.DamageEnemy((int)bleedDamage);
            Debug.Log($"Bleed! Enemy HP: {BattleManager.Instance.enemyHP}");
            elapsed += bleedInterval;
        }
        isBleeding = false;
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
        isAmplified = true;
        Debug.Log("AMPLIFIED! Next card x2");
    }

    public float ConsumeAmplify()
    {
        if (!isAmplified) return 1f;
        isAmplified = false;
        return amplifyMultiplier;
    }

    // SHIELD
    public void ApplyShield(int amount)
    {
        shieldHP += amount;
        Debug.Log($"Shield: {shieldHP} HP");
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