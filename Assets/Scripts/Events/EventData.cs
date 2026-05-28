using System.Collections.Generic;
using UnityEngine;

public enum EventAct
{
    Act1_City,
    Act2_Middle,
    Act3_RuinedCity,
    AnyAct
}

[CreateAssetMenu(fileName = "NewEvent", menuName = "MSA/Event")]
public class EventData : ScriptableObject
{
    [Header("Identity")]
    public string eventID;
    public string eventTitle;
    public EventAct availableInAct = EventAct.AnyAct;

    [Header("Content")]
    [TextArea(3, 8)]
    public string description;
    public Sprite eventImage;

    [Header("Choices")]
    public List<EventChoice> choices = new List<EventChoice>();
}