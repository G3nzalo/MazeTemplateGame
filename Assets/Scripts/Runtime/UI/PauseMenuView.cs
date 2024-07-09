using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _saveGame;
        [SerializeField] private Button _loadGame;
        [SerializeField] private Button _backToMenuButton;
        [SerializeField] private Button _quitGame;

        private InGameMenuMediator _mediator;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(OnResumeButton);
            _saveGame.onClick.AddListener(OnSaveGame);
            _loadGame.onClick.AddListener(OnLoadGame);
            _backToMenuButton.onClick.AddListener(OnBackToMenuPressed);
            _quitGame.onClick.AddListener(OnQuitGamePressed);
        }

        public void Configure(InGameMenuMediator mediator)
        {
            _mediator = mediator;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnBackToMenuPressed()
        {
            _mediator.OnBackToMenuPressed();
        }

        private void OnResumeButton()
        {
            _mediator.OnResumeButton();
        }

        private void OnSaveGame()
        {
            _mediator.OnSaveGame();
        }

        private void OnLoadGame()
        {
            _mediator.OnLoadGame();
        }

        private void OnQuitGamePressed()
        {
            _mediator.OnQuitGame();
        }

    }
}
