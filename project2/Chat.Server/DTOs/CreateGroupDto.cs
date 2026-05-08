namespace Chat.Server.DTOs;

public class CreateGroupDto : BaseMessageDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}
