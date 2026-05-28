using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    // Состояние карты
    public Dictionary<int, POISavedData> mapStateByPOI = new Dictionary<int, POISavedData>();
    public int currentPOI_ID = -1;
    public int visitCount = 0;
    public int pendingPOI_ID = -1;

    // Использованные события — никогда не повторяются
    public HashSet<string> usedEventIDs = new HashSet<string>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsEventUsed(string eventID)
    {
        return usedEventIDs.Contains(eventID);
    }

    public void MarkEventUsed(string eventID)
    {
        if (!string.IsNullOrEmpty(eventID))
            usedEventIDs.Add(eventID);
    }
}

[System.Serializable]
public class POISavedData
{
    public POIType type;
    public POIState state;
}