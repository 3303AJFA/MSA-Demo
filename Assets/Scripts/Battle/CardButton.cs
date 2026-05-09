using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CardButton : MonoBehaviour
{
    public CardData card;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        if (card == null) return;
        if (Keyboard.current[card.hotkey].wasPressedThisFrame)
            OnClick();
    }

    void OnClick()
    {
        ComboSystem.Instance.OnCardPressed(card);
    }
}