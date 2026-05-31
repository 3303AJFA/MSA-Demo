using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    // Использованные события — никогда не повторяются (переживает рестарт боя, теряется при выходе из игры)
    public HashSet<string> usedEventIDs = new HashSet<string>();

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
}
