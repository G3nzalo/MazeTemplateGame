using Patterns.Behaviour.Command;
using System.Threading.Tasks;

namespace Maze.Runtime.Commands
{
    public class ShowScreenFadeCommand :Command
    {
        public async Task Execute()
        {
            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.GetService<ScreenFade>().Show();
            await Task.Delay(200);
        }
    }
}
