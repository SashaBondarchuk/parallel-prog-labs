using System.Net.WebSockets;
using System.Text;
using Chat.Server.DTOs;
using Chat.Server.Enums;
using Chat.Server.Models;
using Chat.Server.Repositories;

namespace Chat.Server.Services;

public class MessageReciever : IMessageReciever
{
    private readonly IConnectionManager _connectionManager;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IOfflineMessageQueue _offlineMessageQueue;
    private readonly IWebSocketSender _webSocketSender;
    private readonly IMessageHandler _messageHandler;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<MessageReciever> _logger;

    public MessageReciever(
        IConnectionManager connectionManager,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IOfflineMessageQueue offlineMessageQueue,
        IWebSocketSender webSocketSender,
        IMessageHandler messageHandler,
        IMessageSerializer serializer,
        ILogger<MessageReciever> logger)
    {
        _connectionManager = connectionManager;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _offlineMessageQueue = offlineMessageQueue;
        _webSocketSender = webSocketSender;
        _messageHandler = messageHandler;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(WebSocket socket, string userName, CancellationToken cancellationToken)
    {
        var user = InitializeUser(userName);
        _connectionManager.AddConnection(userName, socket);

        var onlineCount = _userRepository.GetOnlineUsers().Count();
        _logger.LogInformation("[+] {UserName} connected. Online: {OnlineCount}", userName, onlineCount);

        try
        {
            await _messageHandler.BroadcastUserStatusAsync(userName, true, cancellationToken);
            await SendOfflineMessagesAsync(socket, userName, cancellationToken);
            await ReceiveMessagesAsync(socket, userName, cancellationToken);
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket error for user {UserName}", userName);
        }
        finally
        {
            await CleanupConnectionAsync(userName, socket, cancellationToken);
        }
    }

    private User InitializeUser(string userName)
    {
        var user = _userRepository.GetByName(userName);
        if (user != null)
        {
            user.Online = true;
            _userRepository.AddOrUpdate(user);
        }
        else
        {
            user = new User { Name = userName, Online = true };
            _userRepository.AddOrUpdate(user);
        }

        return user;
    }

    private async Task SendOfflineMessagesAsync(WebSocket socket, string userName, CancellationToken cancellationToken)
    {
        var messages = _offlineMessageQueue.DequeueAll(userName);
        foreach (var message in messages)
        {
            await _webSocketSender.SendAsync(socket, message, cancellationToken);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket socket, string userName, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 4];
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

        while (!result.CloseStatus.HasValue)
        {
            var incomingMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await ProcessIncomingMessageAsync(socket, userName, incomingMessage, cancellationToken);

            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        }
    }

    private async Task ProcessIncomingMessageAsync(WebSocket socket, string userName, string payload, CancellationToken cancellationToken)
    {
        var baseMessage = _serializer.Deserialize<BaseMessageDto>(payload);
        if (baseMessage == null)
        {
            _logger.LogWarning("Failed to deserialize message from {UserName}", userName);
            return;
        }

        switch (baseMessage.Type)
        {
            case MessageType.GetUsers:
            case MessageType.GetGroups:
                await HandleInitialRequestsAsync(socket, baseMessage, userName, cancellationToken);
                break;

            case MessageType.Message:
                var chatMessage = _serializer.Deserialize<ChatMessageDto>(payload);
                if (chatMessage != null)
                {
                    await _messageHandler.HandleChatMessageAsync(userName, chatMessage, cancellationToken);
                }
                break;

            case MessageType.CreateGroup:
                var createGroup = _serializer.Deserialize<CreateGroupDto>(payload);
                if (createGroup != null)
                {
                    await _messageHandler.HandleCreateGroupAsync(userName, createGroup, cancellationToken);
                }
                break;

            default:
                _logger.LogWarning("Unknown message type: {MessageType}", baseMessage.Type);
                break;
        }
    }
    
    private async Task HandleInitialRequestsAsync(WebSocket socket, BaseMessageDto baseMessage, string userName, CancellationToken cancellationToken)
    {
        switch (baseMessage.Type)
        {
            case MessageType.GetUsers:
                await SendUsersListAsync(socket, cancellationToken);
                break;
            case MessageType.GetGroups:
                await SendGroupsListAsync(socket, userName, cancellationToken);
                break;
        }
    }
    
    private async Task SendUsersListAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var users = _userRepository.GetAll();
        var usersListDto = new UsersListDto
        {
            Type = MessageType.UsersList,
            Users = users
        };

        await _webSocketSender.SendAsync(socket, usersListDto, cancellationToken);
    }

    private async Task SendGroupsListAsync(WebSocket socket, string userName, CancellationToken cancellationToken)
    {
        var groups = _groupRepository.GetGroupsForUser(userName);
        var groupsListDto = new GroupsListDto
        {
            Type = MessageType.GroupsList,
            Groups = groups
        };

        await _webSocketSender.SendAsync(socket, groupsListDto, cancellationToken);
    }

    private async Task CleanupConnectionAsync(string userName, WebSocket socket, CancellationToken cancellationToken)
    {
        _userRepository.SetOnlineStatus(userName, false);
        _connectionManager.RemoveConnection(userName);

        _logger.LogInformation("[-] {UserName} disconnected", userName);

        await _messageHandler.BroadcastUserStatusAsync(userName, false, CancellationToken.None);

        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", cancellationToken);
        }
    }
}
