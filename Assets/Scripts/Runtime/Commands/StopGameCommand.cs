using System.Threading.Tasks;
using Patterns.Behaviour.Command;
using UnityEngine;

namespace Maze.Runtime.Commands
{
    public class StopGameCommand : Command
    {
        public Task Execute()
        {
            Time.timeScale = 0;
            return Task.CompletedTask;
        }
    }
}

