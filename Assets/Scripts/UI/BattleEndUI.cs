using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleEndUI : MonoBehaviour
{
    public static BattleEndUI Instance;

    [Header("References")]
    public GameObject panelRoot;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI rewardText;
    public Button continueButton;

    void Awake() => Instance = this;

    void Start()
    {
        panelRoot.SetActive(false);
        continueButton.onClick.AddListener(OnContinue);
    }

    public void ShowVictory()
    {
        titleText.text = "VICTORY";
        rewardText.text = "+25 Memories";
        panelRoot.SetActive(true);

        // Здесь позже добавим начисление награды
    }

    void OnContinue()
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.ReturnToMap();
        else
            Debug.LogWarning("SceneFlow not found!");
    }
}