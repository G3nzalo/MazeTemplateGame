using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateController : MonoBehaviour
{
    public enum GameStates
    {
        Playing,
        Victory
    }

    private GameState _currentState;

    private Dictionary<GameStates, GameState> _idToState;

    private void Start()
    {
        var stopGameCommand = new StopGameCommand();

        _idToState = new Dictionary<GameStates, GameState>
                         {
                             {GameStates.Playing, new PlayingState()},
                             {GameStates.Victory, new VictoryState(stopGameCommand)},
                         };

        _currentState = GetState(GameStates.Playing);
        _currentState.Start(ChangeToNextState);
    }

    private async void ChangeToNextState(GameStates nextState)
    {
        await Task.Yield();
        _currentState.Stop();
        _currentState = GetState(nextState);
        _currentState.Start(ChangeToNextState);
    }

    public void Reset()
    {
        ChangeToNextState(GameStates.Playing);
    }

    private GameState GetState(GameStates gameState)
    {
        return _idToState[gameState];
    }

}
