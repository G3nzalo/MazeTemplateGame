using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface EventQueue
{
    void Subscribe(EventIds eventId, EventObserver eventObserver);
    void Unsubscribe(EventIds eventId, EventObserver eventObserver);
    void EnqueueEvent(EventData eventData);
}
