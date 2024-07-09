using UnityEngine;

public class ScoreSystem : MonoSingleton<ScoreSystem>
{
    [SerializeField] private GameDataConfiguration _configuration;

    int _score = 0;

    public string GetScore => _score.ToString();

    public void OnCollectedCoin() => AddScore();
    public string OnResetScore() => ResetScore();

    private void AddScore()
    {
        _score += _configuration.ScorePerCoin;
    }

    private string ResetScore()
    {
        this._score = 0;
        return _score.ToString();
    }

}
