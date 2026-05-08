using Chat.Server.DTOs;
using Chat.Server.Enums;
using Chat.Server.Models;
using Chat.Server.Repositories;

namespace Chat.Server.Services;

public class MessageHandler : IMessageHandler
{
    private readonly IConnectionManager _connectionManager;
    private readonly IUserRepository _userRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IOfflineMessageQueue _offlineMessageQueue;
    private readonly IWebSocketSender _webSocketSender;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        IConnectionManager connectionManager,
        IUserRepository userRepository,
        IGroupRepository groupRepository,
        IOfflineMessageQueue offlineMessageQueue,
        IWebSocketSender webSocketSender,
        IMessageSerializer serializer,
        ILogger<MessageHandler> logger)
    {
        _connectionManager = connectionManager;
        _userRepository = userRepository;
        _groupRepository = groupRepository;
        _offlineMessageQueue = offlineMessageQueue;
        _webSocketSender = webSocketSender;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task HandleChatMessageAsync(string senderName, ChatMessageDto message, CancellationToken cancellationToken)
    {
        if (message.ToType == ChatTargetType.Group)
        {
            await HandleGroupMessageAsync(senderName, message, cancellationToken);
        }
        else
        {
            await HandleDirectMessageAsync(senderName, message, cancellationToken);
        }
    }

    private async Task HandleDirectMessageAsync(string senderName, ChatMessageDto message, CancellationToken cancellationToken)
    {
        var recipient = _userRepository.GetByName(message.To);
        if (recipient == null)
        {
            _logger.LogWarning("Recipient {RecipientName} not found", message.To);
            return;
        }

        var messageDto = new MessageReceivedDto
        {
            From = senderName,
            Content = message.Content,
            Attachments = message.Attachments
        };

        var recipientSocket = _connectionManager.GetConnection(message.To);
        if (recipientSocket != null)
        {
            await _webSocketSender.SendAsync(recipientSocket, messageDto, cancellationToken);
        }
        else
        {
            _offlineMessageQueue.Enqueue(message.To, _serializer.Serialize(messageDto));
            _logger.LogInformation("User {RecipientName} is offline. Message queued.", message.To);
        }
    }

    private async Task HandleGroupMessageAsync(string senderName, ChatMessageDto message, CancellationToken cancellationToken)
    {
        var group = _groupRepository.GetById(message.To);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found", message.To);
            return;
        }

        var messageDto = new MessageReceivedDto
        {
            From = senderName,
            Content = message.Content,
            GroupId = group.Id,
            GroupName = group.Name,
            Attachments = message.Attachments
        };

        var serializedMessage = _serializer.Serialize(messageDto);

        foreach (var memberName in group.Members.Where(m => m != senderName))
        {
            var memberSocket = _connectionManager.GetConnection(memberName);
            if (memberSocket != null)
            {
                await _webSocketSender.SendAsync(memberSocket, serializedMessage, cancellationToken);
            }
            else
            {
                _offlineMessageQueue.Enqueue(memberName, serializedMessage);
            }
        }
    }

    public async Task HandleCreateGroupAsync(string creatorName, CreateGroupDto createGroup, CancellationToken cancellationToken)
    {
        var group = new Group
        {
            Id = Guid.NewGuid().ToString(),
            Name = createGroup.Name,
            Members = new List<string> { creatorName }
        };

        if (createGroup.Members != null && createGroup.Members.Any())
        {
            group.Members.AddRange(createGroup.Members.Where(m => m != creatorName));
        }

        _groupRepository.Add(group);
        _logger.LogInformation("Group {GroupName} created by {CreatorName} with {MemberCount} members",
            group.Name, creatorName, group.Members.Count);

        var groupCreatedDto = new GroupCreatedDto
        {
            Type = MessageType.GroupCreated,
            Group = group
        };

        // Notify all group members
        foreach (var memberName in group.Members)
        {
            var memberSocket = _connectionManager.GetConnection(memberName);
            if (memberSocket != null)
            {
                await _webSocketSender.SendAsync(memberSocket, groupCreatedDto, cancellationToken);
            }
        }
    }

    public async Task BroadcastUserStatusAsync(string userName, bool isOnline, CancellationToken cancellationToken)
    {
        var statusDto = new UserStatusDto
        {
            Type = isOnline ? MessageType.UserJoined : MessageType.UserLeft,
            User = userName
        };

        var connections = _connectionManager.GetConnectionsExcept(userName);
        var sockets = connections.Select(c => c.socket);

        await _webSocketSender.BroadcastAsync(sockets, statusDto, cancellationToken);
    }
}
