using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventDatabase", menuName = "MSA/Event Database")]
public class EventDatabase : ScriptableObject
{
    public List<EventData> allEvents = new List<EventData>();

    public EventData GetRandomFor(EventAct act)
    {
        var pool = new List<EventData>();
        foreach (var ev in allEvents)
        {
            if (ev.availableInAct != act && ev.availableInAct != EventAct.AnyAct)
                continue;

            if (GameState.Instance != null && GameState.Instance.IsEventUsed(ev.eventID))
                continue;

            pool.Add(ev);
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("No unused events left for this act!");
            return null;
        }

        return pool[Random.Range(0, pool.Count)];
    }
}