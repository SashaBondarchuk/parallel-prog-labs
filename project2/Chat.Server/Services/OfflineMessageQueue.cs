using System.Collections.Concurrent;

namespace Chat.Server.Services;

public class OfflineMessageQueue : IOfflineMessageQueue
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _queues = new();

    public void Enqueue(string userName, string message)
    {
        var queue = _queues.GetOrAdd(userName, _ => new ConcurrentQueue<string>());
        queue.Enqueue(message);
    }

    public IEnumerable<string> DequeueAll(string userName)
    {
        if (!_queues.TryGetValue(userName, out var queue))
        {
            return Enumerable.Empty<string>();
        }

        var messages = new List<string>();
        while (queue.TryDequeue(out var message))
        {
            messages.Add(message);
        }

        return messages;
    }

    public bool HasMessages(string userName)
    {
        return _queues.TryGetValue(userName, out var queue) && !queue.IsEmpty;
    }
}
