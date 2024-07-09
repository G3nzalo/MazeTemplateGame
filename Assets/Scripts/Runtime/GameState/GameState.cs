using System;

namespace Maze.Runtime.GameStates
{
    public interface GameState
    {
        void Start(Action<GameStateController.GameStates> onEndedCallback);
        void Stop();
    }
}
