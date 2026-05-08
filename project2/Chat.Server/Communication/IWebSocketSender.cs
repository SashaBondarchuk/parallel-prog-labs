using System.Net.WebSockets;

namespace Chat.Server.Services;

public interface IWebSocketSender
{
    Task SendAsync(WebSocket socket, string message, CancellationToken cancellationToken = default);
    Task SendAsync<T>(WebSocket socket, T obj, CancellationToken cancellationToken = default);
    Task BroadcastAsync(IEnumerable<WebSocket> sockets, string message, CancellationToken cancellationToken = default);
    Task BroadcastAsync<T>(IEnumerable<WebSocket> sockets, T obj, CancellationToken cancellationToken = default);
}
