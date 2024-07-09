using Maze.Runtime.Entities;
using Maze.Runtime.GameStates;
using Maze.Runtime.UI;
using UnityEngine;

namespace Core.Installers
{
    public class GameInstaller : GeneralInstaller
    {
        [SerializeField] private PlayerInstaller _playerInstaller;
        [SerializeField] private GameStateController _gameStateController;
        [SerializeField] private ScreenFade _screenFade;


        protected override void DoStart()
        {
        }

        protected override void DoInstallDependencies()
        {
            InstallPlayerFactory();
            ServiceLocator.Instance.RegisterService(_playerInstaller);
            ServiceLocator.Instance.RegisterService(_gameStateController);
            ServiceLocator.Instance.RegisterService(_screenFade);

        }

        private void InstallPlayerFactory()
        {
            var playerFactory = new PlayerFactory();
            ServiceLocator.Instance.RegisterService(playerFactory);
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance.UnregisterService<PlayerInstaller>();
            ServiceLocator.Instance.UnregisterService<GameStateController>();
            ServiceLocator.Instance.UnregisterService<ScreenFade>();
            ServiceLocator.Instance.UnregisterService<PlayerFactory>();
        }
    }
}
