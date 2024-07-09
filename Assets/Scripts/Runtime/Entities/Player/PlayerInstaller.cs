using UnityEngine;

namespace Maze.Runtime.Entities
{
    public class PlayerInstaller : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private PlayerToSpawnConfiguration _playerToSpawnConfiguration;

        private PlayerBuilder _playerBuilder;

        private void Start()
        {
            var playerFactory = ServiceLocator.Instance.GetService<PlayerFactory>();

            _playerBuilder = playerFactory.Create()
                                      .WithConfiguration(_playerToSpawnConfiguration)
                                      .WithPosition(_playerToSpawnConfiguration.Position)
                                      .WithRotation(_playerToSpawnConfiguration.Rotation);
            SetInput(_playerBuilder);
        }

        private void SetInput(PlayerBuilder playerBuilder)
        {
            playerBuilder.WithInputMode(PlayerBuilder.InputMode.Unity);

        }

        public void SpawnPlayer()
        {
            _playerBuilder.Build(_prefab);
        }

    }

}

