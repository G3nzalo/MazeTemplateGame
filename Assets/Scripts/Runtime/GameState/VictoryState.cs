using System;
using Maze.Runtime.Entities;
using Patterns.Behaviour.Command;

namespace Maze.Runtime.GameStates
{
    public class VictoryState : GameState
    {
        private readonly Command _stopGame;

        public VictoryState(Command finishGame)
        {
            _stopGame = finishGame;
        }

        public void Start(Action<GameStateController.GameStates> onEndedCallback)
        {
            ServiceLocator.Instance.GetService<CommandQueue>()
                          .AddCommand(_stopGame);
        }

        public void Stop()
        {
            var playerMediator = ServiceLocator.Instance.GetService<PlayerMediator>();
            playerMediator.LeavePlayer();
        }

    }

}
