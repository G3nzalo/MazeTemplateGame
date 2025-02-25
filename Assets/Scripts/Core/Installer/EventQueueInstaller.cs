using Maze.Runtime.Events;
using UnityEngine;

namespace Core.Installers
{
    public class EventQueueInstaller : Installer
    {
        [SerializeField] private EventQueueImpl _eventQueue;
        public override void Install(ServiceLocator serviceLocator)
        {
            DontDestroyOnLoad(_eventQueue.gameObject);
            serviceLocator.RegisterService<EventQueue>(_eventQueue);
        }
    }
}

