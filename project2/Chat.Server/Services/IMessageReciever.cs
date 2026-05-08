using System.Net.WebSockets;

namespace Chat.Server.Services;

public interface IMessageReciever
{
    Task HandleConnectionAsync(WebSocket socket, string userName, CancellationToken cancellationToken);
}
