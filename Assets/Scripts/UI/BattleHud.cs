using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleHUD : MonoBehaviour
{
    public TextMeshProUGUI voltageText;
    public Slider enemyHPSlider;

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;

        voltageText.text = $"Voltage: {Mathf.FloorToInt(bm.currentVoltage)}/{bm.maxVoltage}";
        enemyHPSlider.value = bm.enemyHP;
    }
}