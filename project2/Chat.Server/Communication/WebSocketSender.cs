using System.Net.WebSockets;
using System.Text;

namespace Chat.Server.Services;

public class WebSocketSender : IWebSocketSender
{
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<WebSocketSender> _logger;

    public WebSocketSender(IMessageSerializer serializer, ILogger<WebSocketSender> logger)
    {
        _serializer = serializer;
        _logger = logger;
    }

    public async Task SendAsync(WebSocket socket, string message, CancellationToken cancellationToken = default)
    {
        if (socket.State != WebSocketState.Open)
        {
            _logger.LogWarning("Attempted to send message to a closed WebSocket");
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message via WebSocket");
        }
    }

    public async Task SendAsync<T>(WebSocket socket, T obj, CancellationToken cancellationToken = default)
    {
        var message = _serializer.Serialize(obj);
        await SendAsync(socket, message, cancellationToken);
    }

    public async Task BroadcastAsync(IEnumerable<WebSocket> sockets, string message, CancellationToken cancellationToken = default)
    {
        var tasks = sockets
            .Where(s => s.State == WebSocketState.Open)
            .Select(s => SendAsync(s, message, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastAsync<T>(IEnumerable<WebSocket> sockets, T obj, CancellationToken cancellationToken = default)
    {
        var message = _serializer.Serialize(obj);
        await BroadcastAsync(sockets, message, cancellationToken);
    }
}
