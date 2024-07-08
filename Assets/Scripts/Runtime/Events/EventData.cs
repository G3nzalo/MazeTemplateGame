using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventData
{
    public readonly EventIds EventId;

    public EventData(EventIds eventId)
    {
        EventId = eventId;
    }
}
