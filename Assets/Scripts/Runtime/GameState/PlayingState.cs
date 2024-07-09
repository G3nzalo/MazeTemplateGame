using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingState : GameState, EventObserver
{
    private Action<GameStateController.GameStates> _onEndedCallback;

    public void Start(Action<GameStateController.GameStates> onEndedCallback)
    {
        _onEndedCallback = onEndedCallback;
        var eventQueue = ServiceLocator.Instance.GetService<EventQueue>();
        eventQueue.Subscribe(EventIds.Victory, this);
    }

    public void Stop()
    {
        var eventQueue = ServiceLocator.Instance.GetService<EventQueue>();
        eventQueue.Unsubscribe(EventIds.Victory, this);
    }

    public void Process(EventData eventData)
    {
        if (eventData.EventId == EventIds.Victory)
        {
            _onEndedCallback?.Invoke(GameStateController.GameStates.Victory);
        }
    }
}
