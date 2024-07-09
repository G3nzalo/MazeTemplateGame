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
    [SerializeField] private ScoreView _scoreViewAmount;
    [SerializeField] private TextMeshProUGUI _scoreTxt;


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
        InitializeSystems();

        ServiceLocator.Instance.GetService<EventQueue>()
                      .Subscribe(EventIds.Victory, this);

        ServiceLocator.Instance.GetService<EventQueue>()
                     .Subscribe(EventIds.CollectedCoin, this);
    }

    private void InitializeSystems()
    {
        _pauseButton.gameObject.SetActive(true);
        _timer.Show();
        _timer.StartTimer();
        _scoreViewAmount.ResetScore();
        _scoreViewAmount.Show();
    }

    private void OnDestroy()
    {
        ServiceLocator.Instance.GetService<EventQueue>()
                      .Unsubscribe(EventIds.Victory, this);

        ServiceLocator.Instance.GetService<EventQueue>()
                     .Unsubscribe(EventIds.CollectedCoin, this);
    }

    private void HideAllMenus()
    {
        _pauseMenuView.Hide();
        _victoryView.Hide();
        _scoreViewAmount.Hide();
        _timer.Hide();
        _scoreTxt.gameObject.SetActive(false);
    }

    private void OnPauseButtonPressed()
    {
        _commandQueue.AddCommand(new PauseGameCommand());
        _pauseButton.gameObject.SetActive(false);
        _timer.Pause();
        _timer.Hide();
        _scoreViewAmount.Hide();
        _scoreTxt.gameObject.SetActive(false);
        _pauseMenuView.Show();
    }

    public void OnBackToMenuPressed()
    {
        _scoreViewAmount.ResetScore();
        _timer.ResetTimer();
        _timer.Hide();
        _scoreViewAmount.Hide();
        _scoreTxt.gameObject.SetActive(false);
        _commandQueue.AddCommand(new LoadSceneCommand("Menu"));
        _pauseButton.gameObject.SetActive(false);
        ResumeGame();
    }

    public void OnQuitGame()
    {

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
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
        _scoreViewAmount.Show();
        _scoreTxt.gameObject.SetActive(true);
        ResumeGame();
    }

    private void ResumeGame()
    {
        _commandQueue.AddCommand(new ResumeGameCommand());
    }

    public void OnHideTimer()
    {
        _timer.Hide();
    }

    public void Process(EventData eventData)
    {
        if (eventData.EventId == EventIds.Victory)
        {
            _pauseButton.gameObject.SetActive(false);
            _scoreViewAmount.ResetScore();
            HideAllMenus();
            _victoryView.Show();
            return;
        }

        if (eventData.EventId == EventIds.CollectedCoin)
        {
            _scoreViewAmount.UpdateScore();
            return;
        }
    }

}