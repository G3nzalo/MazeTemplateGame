using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Maze.Runtime.UI
{
    public class VictoryView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _winText;
        [SerializeField] private Button _quitGameButton;
        [SerializeField] private Button _backToMenuButton;
        private InGameMenuMediator _mediator;

        public void Configure(InGameMenuMediator mediator)
        {
            _mediator = mediator;
            _quitGameButton.onClick.AddListener(OnQuitGamePressed);
            _backToMenuButton.onClick.AddListener(OnBackToMenuPressed);
        }

        public void Show()
        {
            _mediator.OnHideTimer();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnBackToMenuPressed()
        {
            RemoveListeners();
            _mediator.OnBackToMenuPressed();
        }

        private void OnQuitGamePressed()
        {
            RemoveListeners();
            _mediator.OnQuitGame();
        }

        private void RemoveListeners()
        {
            _quitGameButton.onClick.RemoveAllListeners();
            _backToMenuButton.onClick.RemoveAllListeners();
        }
    }

}
