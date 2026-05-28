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

    [Header("Sprites (Enemy override, optional)")]
    [Tooltip("Если пусто — используются Default спрайты выше")]
    public Sprite enemySpriteActive;
    public Sprite enemySpriteEmpty;
    public Sprite enemySpriteDamaged;

    private int lastPlayerHP = -1;
    private int lastEnemyHP = -1;

    void Awake() => Instance = this;

    void Update()
    {
        if (BattleManager.Instance == null) return;

        if (BattleManager.Instance.playerHP != lastPlayerHP)
        {
            UpdateSegments(
                playerSegments,
                BattleManager.Instance.playerHP,
                playerMaxHP,
                playerDrainReversed,
                spriteActive, spriteEmpty, spriteDamaged
            );
            if (playerHPText != null) playerHPText.text = $"{BattleManager.Instance.playerHP}";
            lastPlayerHP = BattleManager.Instance.playerHP;
        }

        if (BattleManager.Instance.enemyHP != lastEnemyHP)
        {
            // Резолвим: если у врага задан override — используем его, иначе дефолт
            Sprite eActive = enemySpriteActive != null ? enemySpriteActive : spriteActive;
            Sprite eEmpty = enemySpriteEmpty != null ? enemySpriteEmpty : spriteEmpty;
            Sprite eDamaged = enemySpriteDamaged != null ? enemySpriteDamaged : spriteDamaged;

            UpdateSegments(
                enemySegments,
                BattleManager.Instance.enemyHP,
                enemyMaxHP,
                enemyDrainReversed,
                eActive, eEmpty, eDamaged
            );
            if (enemyHPText != null) enemyHPText.text = $"{BattleManager.Instance.enemyHP}";
            lastEnemyHP = BattleManager.Instance.enemyHP;
        }
    }

    void UpdateSegments(List<Image> segments, int currentHP, int maxHP, bool reversed,
                        Sprite sActive, Sprite sEmpty, Sprite sDamaged)
    {
        if (segments == null || segments.Count == 0) return;

        float percent = (float)currentHP / maxHP;
        int activeCount = Mathf.CeilToInt(percent * segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            Image seg = segments[i];
            // При reversed считаем "активность" от конца массива — гаснут сегменты с начала
            int effectiveIndex = reversed ? (segments.Count - 1 - i) : i;
            bool isActive = effectiveIndex < activeCount;
            bool wasActive = seg.sprite == sActive;

            seg.DOKill();
            seg.transform.DOKill();

            if (isActive)
            {
                // Активный сегмент
                if (!wasActive)
                {
                    seg.sprite = sActive;
                    seg.transform.localScale = Vector3.one;
                }
            }
            else if (wasActive)
            {
                // Только что погас — анимация удара
                StartCoroutine(DamageFlash(seg, sDamaged, sEmpty));
            }
            else
            {
                // Уже погасший
                seg.sprite = sEmpty;
            }
        }
    }

    IEnumerator DamageFlash(Image seg, Sprite sDamaged, Sprite sEmpty)
    {
        // Вспышка
        seg.sprite = sDamaged;
        seg.transform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 8, 0.5f);

        yield return new WaitForSeconds(0.2f);

        // Переход к пустому
        seg.sprite = sEmpty;
        seg.transform.DOScale(0.85f, 0.3f);
    }
}