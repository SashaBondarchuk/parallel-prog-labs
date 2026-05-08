using Chat.Server.DTOs;

namespace Chat.Server.Services;

public interface IMessageHandler
{
    Task HandleChatMessageAsync(string senderName, ChatMessageDto message, CancellationToken cancellationToken);
    Task HandleCreateGroupAsync(string creatorName, CreateGroupDto createGroup, CancellationToken cancellationToken);
    Task BroadcastUserStatusAsync(string userName, bool isOnline, CancellationToken cancellationToken);
}
