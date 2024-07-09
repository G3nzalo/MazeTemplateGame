using System.Threading.Tasks;
using Patterns.Behaviour.Command;
using UnityEngine;

namespace Maze.Runtime.Commands
{
    public class ResumeGameCommand : Command
    {
        public Task Execute()
        {
            Time.timeScale = 1;
            return Task.CompletedTask;
        }
    }
}
