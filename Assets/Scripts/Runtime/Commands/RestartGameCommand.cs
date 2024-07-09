using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Patterns.Behaviour.Command;
using UnityEngine;

namespace Maze.Runtime.Commands
{
    public class RestartBattleCommand : Command
    {
        public async Task Execute()
        {
            ServiceLocator.Instance.GetService<EventQueue>()
                          .EnqueueEvent(new EventData(EventIds.Restart));
            await new StopGameCommand().Execute();
            await new StartGameCommand().Execute();
        }
    }
}

