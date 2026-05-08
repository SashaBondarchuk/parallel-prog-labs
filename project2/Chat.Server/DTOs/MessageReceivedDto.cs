using Chat.Server.Enums;

namespace Chat.Server.DTOs;

public class MessageReceivedDto
{
    public MessageType Type { get; } = MessageType.Message;
    public string From { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? GroupId { get; set; }
    public string? GroupName { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
}
