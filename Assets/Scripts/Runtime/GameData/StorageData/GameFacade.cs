using Core.DataStorage;
using Maze.Runtime.Score;
using Patterns.Structural.Adapter;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Maze.Runtime.GameData
{
    public class GameFacade : MonoBehaviour
    {
        Vector3 _position;
        Quaternion _rotation;
        int _score;

        private DataStore _dataStore;

        public void Save() => SaveGame();
        public void Load() => LoadGame();

        private void Start()
        {
            _dataStore = new PlayerPrefsDataStoreAdapter();
        }

        private void SaveGame()
        {
            GetDataToSave();
            SaveGameData();
            Debug.Log("Save Game Successful");
        }

        private void GetDataToSave()
        {
            _score = ScoreSystem.Instance.GetScoreToStorage;
            GameObject player = GameObject.FindWithTag("Player");
            _position = player.transform.position;
            _rotation = player.transform.rotation;
        }

        private void SaveGameData()
        {
            var saveData = new SaveData(_position, _rotation, _score);
            _dataStore.SetData(saveData, "SaveData");
        }

        public void LoadGame()
        {
            var saveData = _dataStore.GetData<SaveData>("SaveData");
            _position = saveData.PlayerPosition;
            _rotation = saveData.PlayerRotation;
            _score = saveData.Score;

            GameObject player = GameObject.FindWithTag("Player");
            player.transform.position = _position;
            player.transform.rotation = _rotation;
            ScoreSystem.Instance.ReloadScoreFromDB(_score);
        }
    }

}
