using Maze.Runtime.Commands;
using Patterns.Behaviour.Command;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _quitButton;

        private void Start()
        {
            _startGameButton.onClick.AddListener(OnStartButtonPressed);
            _quitButton.onClick.AddListener(OnQuitButtonPressed);
        }

        private void OnStartButtonPressed()
        {
            _startGameButton.onClick.RemoveAllListeners();
            LoadGameSceneCommand loadGameSceneCommand = new LoadGameSceneCommand();

            ServiceLocator.Instance.GetService<CommandQueue>()
                          .AddCommand(loadGameSceneCommand);
        }

        private void OnQuitButtonPressed()
        {
            _quitButton.onClick.RemoveAllListeners();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }


    }
}
