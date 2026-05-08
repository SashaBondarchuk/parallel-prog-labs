namespace Chat.Server.Services;

public interface IMessageSerializer
{
    string Serialize<T>(T obj);
    T? Deserialize<T>(string json);
}
