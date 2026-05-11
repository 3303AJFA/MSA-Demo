using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoltageBarUI : MonoBehaviour
{
    public RectTransform fill;       // VB_Fill
    public TextMeshProUGUI text;     // VB_Text

    private float maxHeight;

    void Start()
    {
        maxHeight = fill.rect.height;
    }

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;

        float percent = bm.currentVoltage / bm.maxVoltage;

        // Меняем высоту через scale (проще чем sizeDelta)
        fill.localScale = new Vector3(1f, percent, 1f);

        text.text = $"{Mathf.FloorToInt(bm.currentVoltage)}%";
    }
}