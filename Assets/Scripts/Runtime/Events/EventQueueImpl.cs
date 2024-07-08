using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventQueueImpl : MonoBehaviour, EventQueue
{
    private class RemoveData
    {
        public EventIds EventId;
        public EventObserver EventObserver;

        public RemoveData(EventIds eventId, EventObserver eventObserver)
        {
            EventId = eventId;
            EventObserver = eventObserver;
        }
    }

    private List<RemoveData> _observersToUnsubscribe;
    private Queue<EventData> _currentEvents;
    private Queue<EventData> _nextEvents;

    private Dictionary<EventIds, List<EventObserver>> _observers;
    private bool _isProcessingEvents;

    private void Awake()
    {
        _observersToUnsubscribe = new List<RemoveData>();
        _currentEvents = new Queue<EventData>();
        _nextEvents = new Queue<EventData>();
        _observers = new Dictionary<EventIds, List<EventObserver>>();
    }

    public void Subscribe(EventIds eventId, EventObserver eventObserver)
    {
        if (!_observers.TryGetValue(eventId, out var eventObservers))
        {
            eventObservers = new List<EventObserver>();
        }

        eventObservers.Add(eventObserver);
        _observers[eventId] = eventObservers;
    }

    public void Unsubscribe(EventIds eventId, EventObserver eventObserver)
    {
        if (_isProcessingEvents)
        {
            var removeData = new RemoveData(eventId, eventObserver);
            _observersToUnsubscribe.Add(removeData);
            return;
        }

        DoUnsubscribe(eventId, eventObserver);
    }

    private void DoUnsubscribe(EventIds eventId, EventObserver eventObserver)
    {
        _observers[eventId].Remove(eventObserver);
    }

    public void EnqueueEvent(EventData eventData)
    {
        _nextEvents.Enqueue(eventData);
    }

    private void LateUpdate()
    {
        ProcessEvents();
    }

    private void ProcessEvents()
    {
        var tempCurrentEvents = _currentEvents;
        _currentEvents = _nextEvents;
        _nextEvents = tempCurrentEvents;

        foreach (var currentEvent in _currentEvents)
        {
            ProcessEvent(currentEvent);
        }

        _currentEvents.Clear();
    }

    private void ProcessEvent(EventData eventData)
    {
        _isProcessingEvents = true;
        if (_observers.TryGetValue(eventData.EventId, out var eventObservers))
        {
            foreach (var eventObserver in eventObservers)
            {
                eventObserver.Process(eventData);
            }
        }
        _isProcessingEvents = false;

        UnsubscribePendingObservers();
    }

    private void UnsubscribePendingObservers()
    {
        foreach (var removeData in _observersToUnsubscribe)
        {
            DoUnsubscribe(removeData.EventId, removeData.EventObserver);
        }
    }
}