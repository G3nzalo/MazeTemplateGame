using System.Collections.Generic;
using System.Threading.Tasks;

namespace Patterns.Behaviour.Command
{
    public class CommandQueue
    {
        public static CommandQueue Instance => _instance ?? (_instance = new CommandQueue());
        private static CommandQueue _instance;

        private readonly Queue<Command> _commandsToExecute;
        private bool _runningCommand;

        private CommandQueue()
        {
            _commandsToExecute = new Queue<Command>();
            _runningCommand = false;
        }

        public void AddCommand(Command commandToEnqueue)
        {
            _commandsToExecute.Enqueue(commandToEnqueue);
            RunNextCommand().WrapErrors();
        }

        private async Task RunNextCommand()
        {
            if (_runningCommand)
            {
                return;
            }

            while (_commandsToExecute.Count > 0)
            {
                _runningCommand = true;
                var commandToExecute = _commandsToExecute.Dequeue();
                await commandToExecute.Execute();
            }

            _runningCommand = false;
        }
    }
}
