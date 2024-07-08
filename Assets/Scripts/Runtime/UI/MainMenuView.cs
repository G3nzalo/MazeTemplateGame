using Maze.Runtime.Commands;
using Patterns.Behaviour.Command;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _quitButton;

        private void Start()
        {
            _startGameButton.onClick.AddListener(OnStartButtonPressed);
            //_loadGameButton.onClick.AddListener(OnLoadButtonPressed);
            _quitButton.onClick.AddListener(OnQuitButtonPressed);
        }

        private void OnStartButtonPressed()
        {
            LoadGameSceneCommand loadGameSceneCommand = new LoadGameSceneCommand();

            ServiceLocator.Instance.GetService<CommandQueue>()
                          .AddCommand(loadGameSceneCommand);
        }

        private void OnLoadButtonPressed()
        {
            LoadGameSceneCommand loadGameSceneCommand = new LoadGameSceneCommand();

            ServiceLocator.Instance.GetService<CommandQueue>()
                          .AddCommand(loadGameSceneCommand);
        }


        private void OnQuitButtonPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }


    }
}
