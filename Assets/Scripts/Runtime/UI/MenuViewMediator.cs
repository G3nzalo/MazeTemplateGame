using Maze.Runtime.Commands;
using Patterns.Behaviour.Command;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MenuViewMediator : MonoBehaviour, InGameMenuMediator, EventObserver
{
    [SerializeField] private Button _pauseButton;
    [SerializeField] private PauseMenuView _pauseMenuView;
    [SerializeField] private VictoryView _victoryView;
    [SerializeField] private Timer _timer;

    public Button PauseButton => _pauseButton;
    private CommandQueue _commandQueue;

    private void Awake()
    {
        _pauseButton.onClick.AddListener(OnPauseButtonPressed);
        _pauseMenuView.Configure(this);
        _victoryView.Configure(this);
    }

    void Start()
    {
        _commandQueue = ServiceLocator.Instance.GetService<CommandQueue>();
        HideAllMenus();
        _pauseButton.gameObject.SetActive(true);
        _timer.Show();
        _timer.StartTimer();

        ServiceLocator.Instance.GetService<EventQueue>()
                      .Subscribe(EventIds.Victory, this);

        //ServiceLocator.Instance.GetService<EventQueue>()
        //              .Subscribe(EventIds.GameOver, this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Instance.GetService<EventQueue>()
                      .Unsubscribe(EventIds.Victory, this);

        //ServiceLocator.Instance.GetService<EventQueue>()
        //              .Unsubscribe(EventIds.GameOver, this);
    }

    private void HideAllMenus()
    {
        _pauseMenuView.Hide();
        _victoryView.Hide();

        //_gameOverView.Hide();
    }

    private void OnPauseButtonPressed()
    {
        _commandQueue.AddCommand(new PauseGameCommand());
        _pauseButton.gameObject.SetActive(false);
        _timer.Pause();
        _timer.Hide();
        _pauseMenuView.Show();
    }

    public void OnBackToMenuPressed()
    {
        _timer.ResetTimer();
        _timer.Hide();
        _commandQueue.AddCommand(new LoadSceneCommand("Menu"));
        _pauseButton.gameObject.SetActive(false);
        ResumeGame();
    }

    public void OnQuitGame()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        _pauseButton.gameObject.SetActive(false);
        Application.Quit();
#endif
    }

    public void OnResumeButton()
    {
        _pauseMenuView.Hide();
        _timer.Show();
        _timer.Unpause();
        _timer.StartTimer(_timer.CurrentTime);
        _pauseButton.gameObject.SetActive(true);
        ResumeGame();
    }

    private void ResumeGame()
    {
        _commandQueue.AddCommand(new ResumeGameCommand());
    }

    public void Process(EventData eventData)
    {
        if (eventData.EventId == EventIds.Victory)
        {
            _pauseButton.gameObject.SetActive(false);
            _victoryView.Show();
            return;
        }

        //if (eventData.EventId == EventIds.GameOver)
        //{
        //    _gameOverView.Show();
        //    return;
        //}
    }
}