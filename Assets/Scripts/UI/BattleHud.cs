using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleHUD : MonoBehaviour
{
    public TextMeshProUGUI voltageText; // можно оставить пустым
    public Slider enemyHPSlider;

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;

        if (voltageText != null)
            voltageText.text = $"Voltage: {Mathf.FloorToInt(bm.currentVoltage)}%";

        if (enemyHPSlider != null)
            enemyHPSlider.value = bm.enemyHP;
    }
}