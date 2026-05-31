using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EventOverlay : MonoBehaviour
{
    public static EventOverlay Instance;

    [Header("References")]
    public GameObject panelRoot;
    public Image eventImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    [Header("Outcome")]
    public GameObject outcomePanel;
    public Image outcomeImage;
    public TextMeshProUGUI outcomeText;
    public Button continueButton;

    [Header("Database")]
    public EventDatabase eventDatabase;
    public EventAct currentAct = EventAct.Act1_City;

    private List<GameObject> activeButtons = new List<GameObject>();

    void Awake() => Instance = this;

    void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (outcomePanel != null) outcomePanel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(Close);
    }

    public void OpenRandom(int poiID)
    {
        if (eventDatabase == null)
        {
            Debug.LogWarning("EventDatabase is not assigned!");
            FallbackToCombat(poiID);
            return;
        }

        var ev = eventDatabase.GetRandomFor(currentAct);
        if (ev == null)
        {
            Debug.Log("No unused events left — falling back to combat.");
            FallbackToCombat(poiID);
            return;
        }
        OpenEvent(ev);
    }

    void FallbackToCombat(int poiID)
    {
        if (SceneFlow.Instance != null)
            SceneFlow.Instance.GoToBattle(poiID);
        else
            Debug.LogWarning("SceneFlow not found!");
    }

    public void OpenMerchant(int poiID)
    {
        Debug.Log("Merchant panel — to be implemented.");
    }

    public void OpenFriend(int poiID)
    {
        Debug.Log("Friend dialogue — to be implemented.");
    }

    public void OpenEvent(EventData data)
    {
        if (GameState.Instance != null)
            GameState.Instance.MarkEventUsed(data.eventID);

        ClearChoices();
        outcomePanel.SetActive(false);

        titleText.text = data.eventTitle;
        descriptionText.text = data.description;

        if (eventImage != null)
        {
            eventImage.sprite = data.eventImage;
            eventImage.enabled = data.eventImage != null;
        }

        foreach (var choice in data.choices)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesContainer);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = choice.choiceText;

            var captured = choice;
            btn.GetComponent<Button>().onClick.AddListener(() => OnChoicePicked(captured));
            activeButtons.Add(btn);
        }

        panelRoot.SetActive(true);
    }

    void OnChoicePicked(EventChoice choice)
    {
        Debug.Log($"Choice: {choice.choiceText}");
        Debug.Log($"HP {choice.hpChange}, Voltage {choice.voltageChange}, Coins {choice.coinsChange}");

        ClearChoices();
        descriptionText.gameObject.SetActive(false);
        outcomePanel.SetActive(true);
        outcomeText.text = choice.outcomeText;

        if (choice.outcomeImage != null)
        {
            outcomeImage.sprite = choice.outcomeImage;
            outcomeImage.enabled = true;
        }
        else if (outcomeImage != null)
        {
            outcomeImage.enabled = false;
        }
    }

    void ClearChoices()
    {
        foreach (var b in activeButtons) Destroy(b);
        activeButtons.Clear();
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        outcomePanel.SetActive(false);
        descriptionText.gameObject.SetActive(true);
    }
}