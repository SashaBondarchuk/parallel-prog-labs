namespace Chat.Server.Services;

public interface IOfflineMessageQueue
{
    void Enqueue(string userName, string message);
    IEnumerable<string> DequeueAll(string userName);
    bool HasMessages(string userName);
}
