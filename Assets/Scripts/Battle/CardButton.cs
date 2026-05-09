using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CardButton : MonoBehaviour
{
    public CardData card;
    private Button button;
    private bool isHeld = false;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        if (card == null) return;

        if (Keyboard.current[card.hotkey].wasPressedThisFrame)
        {
            isHeld = true;
            ComboSystem.Instance.OnCardPressed(card);
        }

        if (Keyboard.current[card.hotkey].wasReleasedThisFrame && isHeld)
        {
            isHeld = false;
            ComboSystem.Instance.OnCardReleased(card);
        }
    }

    void OnClick()
    {
        ComboSystem.Instance.OnCardPressed(card);
        ComboSystem.Instance.OnCardReleased(card);
    }
}