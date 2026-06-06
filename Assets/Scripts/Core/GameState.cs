using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    // Использованные события — никогда не повторяются (переживает рестарт боя, теряется при выходе из игры)
    public HashSet<string> usedEventIDs = new HashSet<string>();

    // Диалоговые флаги — рантайм-стор булевой памяти диалогов: «сделан ли выбор X ранее».
    // Используется DialogueRunner для проверки условий входа в варианты и записи эффектов выбора.
    // НЕ skill-check, не статы — просто булевая память.
    public HashSet<string> dialogueFlags = new HashSet<string>();

    // TODO (заход про систему переходов между сценами): хранилище состояния мира —
    // посещённые сцены, прогресс района/локации, и т.п. Пока пусто.

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsEventUsed(string eventID) => usedEventIDs.Contains(eventID);

    public void MarkEventUsed(string eventID)
    {
        if (!string.IsNullOrEmpty(eventID))
            usedEventIDs.Add(eventID);
    }

    // --- Dialogue flags API ---

    public bool GetFlag(string name)
    {
        return !string.IsNullOrEmpty(name) && dialogueFlags.Contains(name);
    }

    public void SetFlag(string name, bool value)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (value) dialogueFlags.Add(name);
        else dialogueFlags.Remove(name);
    }

    public void ClearAllFlags()
    {
        dialogueFlags.Clear();
    }
}
