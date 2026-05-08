using System.Text;
using Chat.Server.Repositories;
using Chat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = Encoding.UTF8;

// Register repositories
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IGroupRepository, GroupRepository>();

// Register services
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<IOfflineMessageQueue, OfflineMessageQueue>();
builder.Services.AddSingleton<IMessageSerializer, MessageSerializer>();
builder.Services.AddSingleton<IWebSocketSender, WebSocketSender>();
builder.Services.AddSingleton<IMessageHandler, MessageHandler>();
builder.Services.AddSingleton<IMessageReciever, MessageReciever>();

// Configure OpenAPI for development
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseWebSockets();

// WebSocket endpoint
app.Map("/chat", async (HttpContext context, IMessageReciever messageReciever, CancellationToken cancellationToken) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket request required", cancellationToken);
        return;
    }

    var userName = context.Request.Query["user"].ToString();

    if (string.IsNullOrWhiteSpace(userName))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("User name is required", cancellationToken);
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    await messageReciever.HandleConnectionAsync(webSocket, userName, cancellationToken);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();
