using Maze.Runtime.UI;
using UnityEngine;

namespace Core.Installers
{
    public class LoadingScreenInstaller : Installer
    {
        [SerializeField] private LoadingScreen _loadingScreen;

        public override void Install(ServiceLocator serviceLocator)
        {
            DontDestroyOnLoad(_loadingScreen.gameObject);
            serviceLocator.RegisterService(_loadingScreen);
        }
    }
}
