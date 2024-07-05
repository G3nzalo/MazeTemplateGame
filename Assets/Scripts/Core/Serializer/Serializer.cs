namespace Core.Serializers
{
    public interface Serializer
    {
        string ToJson<T>(T data);
        T FromJson<T>(string data);
    }
}
