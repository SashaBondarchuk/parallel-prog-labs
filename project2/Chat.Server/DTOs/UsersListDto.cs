using Chat.Server.Models;

namespace Chat.Server.DTOs;

public class UsersListDto : BaseMessageDto
{
    public IEnumerable<User> Users { get; set; } = Enumerable.Empty<User>();
}
