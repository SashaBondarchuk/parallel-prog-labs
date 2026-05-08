using Chat.Server.Models;

namespace Chat.Server.DTOs;

public class GroupCreatedDto : BaseMessageDto
{
    public Group Group { get; set; } = null!;
}
