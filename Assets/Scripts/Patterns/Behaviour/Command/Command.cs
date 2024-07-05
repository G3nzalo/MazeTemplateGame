using System.Threading.Tasks;

namespace Patterns.Behaviour.Command
{
    public interface Command
    {
        Task Execute();
    }
}
