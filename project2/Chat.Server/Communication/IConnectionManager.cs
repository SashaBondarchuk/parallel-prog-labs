using System.Net.WebSockets;

namespace Chat.Server.Services;

public interface IConnectionManager
{
    void AddConnection(string userName, WebSocket socket);
    void RemoveConnection(string userName);
    WebSocket? GetConnection(string userName);
    IEnumerable<(string userName, WebSocket socket)> GetAllConnections();
    IEnumerable<(string userName, WebSocket socket)> GetConnectionsExcept(string userName);
}
