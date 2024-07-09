using Maze.Runtime.GameData;
using Maze.Tools;
using UnityEngine;

namespace Maze.Runtime.Score
{
    public class ScoreSystem : MonoSingleton<ScoreSystem>
    {
        [SerializeField] private GameDataConfiguration _configuration;

        int _score = 0;

        public int GetScoreToStorage => _score;
        public string GetScore => _score.ToString();

        public void ReloadScoreFromDB(int scoreSaved) => SetScoreFromDb(scoreSaved);
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

        private void SetScoreFromDb(int scoreSaved)
        {
            _score = scoreSaved;
        }
    }

}
