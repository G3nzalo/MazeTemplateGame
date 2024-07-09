using Maze.Runtime.Entities;
using Maze.Runtime.GameStates;
using Patterns.Behaviour.Command;
using System.Threading.Tasks;
using UnityEngine;

namespace Maze.Runtime.Commands
{
    public class StartGameCommand : Command
    {
        public async Task Execute()
        {
            await new ShowScreenFadeCommand().Execute();

            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.GetService<GameStateController>().Reset();
            serviceLocator.GetService<PlayerInstaller>().SpawnPlayer();

            await new HideScreenFadeCommand().Execute();
        }
    }
}
