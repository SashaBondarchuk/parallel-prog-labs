using Chat.Server.Enums;

namespace Chat.Server.DTOs;

public class ChatMessageDto : BaseMessageDto
{
    public string To { get; set; } = string.Empty;
    public ChatTargetType ToType { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<AttachmentDto> Attachments { get; set; } = new();
}
