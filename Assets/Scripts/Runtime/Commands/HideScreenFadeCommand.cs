using Maze.Runtime.UI;
using Patterns.Behaviour.Command;
using System.Threading.Tasks;

namespace Maze.Runtime.Commands
{
    public class HideScreenFadeCommand : Command
    {
        public async Task Execute()
        {
            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.GetService<ScreenFade>().Hide();
            await Task.Delay(200);
        }
    }
}
