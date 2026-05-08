using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Chat.Server.Services;

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public void AddConnection(string userName, WebSocket socket)
    {
        _connections.AddOrUpdate(userName, socket, (_, _) => socket);
    }

    public void RemoveConnection(string userName)
    {
        _connections.TryRemove(userName, out _);
    }

    public WebSocket? GetConnection(string userName)
    {
        _connections.TryGetValue(userName, out var socket);
        return socket;
    }

    public IEnumerable<(string userName, WebSocket socket)> GetAllConnections()
    {
        return _connections.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    }

    public IEnumerable<(string userName, WebSocket socket)> GetConnectionsExcept(string userName)
    {
        return _connections
            .Where(kvp => kvp.Key != userName)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }
}
