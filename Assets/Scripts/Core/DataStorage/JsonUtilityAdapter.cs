using UnityEngine;

namespace Core.Serializers
{
    public class JsonUtilityAdapter : Serializer
    {
        public string ToJson<T>(T data)
        {
            return JsonUtility.ToJson(data);
        }

        public T FromJson<T>(string data)
        {
            return JsonUtility.FromJson<T>(data);
        }
    }
}