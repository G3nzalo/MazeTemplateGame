using System;
using Patterns.Behaviour.Command;

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
            ServiceLocator.Instance.GetService<EventQueue>().
            EnqueueEvent(new EventData(EventIds.Victory));
        }

        public void Stop()
        {
        }
    }
