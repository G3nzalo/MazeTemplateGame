using System.Threading.Tasks;
using Maze.Runtime.Events;
using Patterns.Behaviour.Command;

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

