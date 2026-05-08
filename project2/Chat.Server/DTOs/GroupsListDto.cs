using Chat.Server.Models;

namespace Chat.Server.DTOs;

public class GroupsListDto : BaseMessageDto
{
    public IEnumerable<Group> Groups { get; set; } = Enumerable.Empty<Group>();
}
