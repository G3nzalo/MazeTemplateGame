using Core.Serializers;
using Patterns.Structural.Adapter;
using UnityEngine;

namespace Core.DataStorage
{
    public class PlayerPrefsDataStoreAdapter : DataStore
    {
        private readonly Serializer _serializer;

        public PlayerPrefsDataStoreAdapter(Serializer serializer)
        {
            _serializer = serializer;
        }

        public void SetData<T>(T data, string name)
        {
            var json = _serializer.ToJson(data);
            PlayerPrefs.SetString(name, json);
            PlayerPrefs.Save();
        }

        public T GetData<T>(string name)
        {
            var json = PlayerPrefs.GetString(name);
            return _serializer.FromJson<T>(json);
        }
    }
}
