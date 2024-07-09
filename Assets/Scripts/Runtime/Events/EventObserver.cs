
namespace Maze.Runtime.Events
{
    public interface EventObserver
    {
        void Process(EventData eventData);
    }

}
