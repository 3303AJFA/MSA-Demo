using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("Player")]
    public List<Image> playerSegments;
    public TextMeshProUGUI playerHPText;
    public int playerMaxHP = 100;
    public bool playerDrainReversed = false;

    [Header("Enemy")]
    public List<Image> enemySegments;
    public TextMeshProUGUI enemyHPText;
    public int enemyMaxHP = 100;
    public bool enemyDrainReversed = true;

    [Header("Sprites (Default / Player)")]
    public Sprite spriteActive;
    public Sprite spriteEmpty;
    public Sprite spriteDamaged;
    public Sprite spriteShield;

    [Header("Sprites (Enemy override, optional)")]
    public Sprite enemySpriteActive;
    public Sprite enemySpriteEmpty;
    public Sprite enemySpriteDamaged;

    private int lastPlayerHP = -1;
    private int lastEnemyHP = -1;
    private int lastShieldHP = -1;

    void Awake() => Instance = this;

    void Update()
    {
        if (BattleManager.Instance == null) return;

        int currentShield = StatusEffect.Instance != null ? StatusEffect.Instance.shieldHP : 0;
        bool shieldChanged = currentShield != lastShieldHP;
        bool playerChanged = BattleManager.Instance.playerHP != lastPlayerHP;

        if (playerChanged || shieldChanged)
        {
            UpdatePlayerSegments(BattleManager.Instance.playerHP, currentShield);
            if (playerHPText != null) playerHPText.text = $"{BattleManager.Instance.playerHP}";
            lastPlayerHP = BattleManager.Instance.playerHP;
            lastShieldHP = currentShield;
        }

        if (BattleManager.Instance.enemyHP != lastEnemyHP)
        {
            Sprite eActive = enemySpriteActive != null ? enemySpriteActive : spriteActive;
            Sprite eEmpty = enemySpriteEmpty != null ? enemySpriteEmpty : spriteEmpty;
            Sprite eDamaged = enemySpriteDamaged != null ? enemySpriteDamaged : spriteDamaged;

            UpdateEnemySegments(BattleManager.Instance.enemyHP, eActive, eEmpty, eDamaged);
            if (enemyHPText != null) enemyHPText.text = $"{BattleManager.Instance.enemyHP}";
            lastEnemyHP = BattleManager.Instance.enemyHP;
        }
    }

    void UpdatePlayerSegments(int currentHP, int shieldHP)
    {
        if (playerSegments == null || playerSegments.Count == 0) return;
        int count = playerSegments.Count;

        float hpPercent = (float)currentHP / playerMaxHP;
        int hpSegments = Mathf.CeilToInt(hpPercent * count);

        float shieldPercent = (float)shieldHP / playerMaxHP;
        int shieldSegments = Mathf.Min(count, Mathf.CeilToInt(shieldPercent * count));

        for (int i = 0; i < count; i++)
        {
            Image seg = playerSegments[i];
            int effectiveIndex = playerDrainReversed ? (count - 1 - i) : i;

            // Shield оверлеит левые N сегментов (поверх HP). Поглощается с правого края.
            bool isShield = effectiveIndex < shieldSegments;
            bool isHP = !isShield && effectiveIndex < hpSegments;

            Sprite target = isShield ? spriteShield
                          : isHP ? spriteActive
                          : spriteEmpty;

            bool wasActive = seg.sprite == spriteActive;
            bool willBeNothing = !isShield && !isHP;

            seg.DOKill();
            seg.transform.DOKill();

            if (wasActive && willBeNothing)
            {
                StartCoroutine(DamageFlash(seg, spriteDamaged, spriteEmpty));
            }
            else
            {
                seg.sprite = target;
                if (seg.transform.localScale != Vector3.one)
                    seg.transform.localScale = Vector3.one;
            }
        }
    }

    void UpdateEnemySegments(int currentHP, Sprite sActive, Sprite sEmpty, Sprite sDamaged)
    {
        if (enemySegments == null || enemySegments.Count == 0) return;
        int count = enemySegments.Count;

        float percent = (float)currentHP / enemyMaxHP;
        int activeCount = Mathf.CeilToInt(percent * count);

        for (int i = 0; i < count; i++)
        {
            Image seg = enemySegments[i];
            int effectiveIndex = enemyDrainReversed ? (count - 1 - i) : i;
            bool isActive = effectiveIndex < activeCount;
            bool wasActive = seg.sprite == sActive;

            seg.DOKill();
            seg.transform.DOKill();

            if (isActive)
            {
                if (!wasActive)
                {
                    seg.sprite = sActive;
                    seg.transform.localScale = Vector3.one;
                }
            }
            else if (wasActive)
            {
                StartCoroutine(DamageFlash(seg, sDamaged, sEmpty));
            }
            else
            {
                seg.sprite = sEmpty;
            }
        }
    }

    IEnumerator DamageFlash(Image seg, Sprite sDamaged, Sprite sEmpty)
    {
        seg.sprite = sDamaged;
        seg.transform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 8, 0.5f);

        yield return new WaitForSeconds(0.2f);

        seg.sprite = sEmpty;
        seg.transform.DOScale(0.85f, 0.3f);
    }
}
