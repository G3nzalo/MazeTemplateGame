using System;

namespace Maze.Runtime.GameData
{
    [Serializable]
    public class SaveData
    {
        public UnityEngine.Vector3 PlayerPosition;
        public UnityEngine.Quaternion PlayerRotation;
        public int Score;

        public SaveData(UnityEngine.Vector3 playerPosition, UnityEngine.Quaternion playerRotation, int score)
        {
            PlayerPosition = playerPosition;
            PlayerRotation = playerRotation;
            Score = score;
        }
    }
}

