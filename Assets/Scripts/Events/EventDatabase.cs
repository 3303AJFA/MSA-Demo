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
            if (ev.availableInAct == act || ev.availableInAct == EventAct.AnyAct)
                pool.Add(ev);
        }

        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }
}