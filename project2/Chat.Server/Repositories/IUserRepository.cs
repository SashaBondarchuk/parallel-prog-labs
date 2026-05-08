using Chat.Server.Models;

namespace Chat.Server.Repositories;

public interface IUserRepository
{
    User? GetByName(string name);
    IEnumerable<User> GetAll();
    IEnumerable<User> GetOnlineUsers();
    void AddOrUpdate(User user);
    bool SetOnlineStatus(string userName, bool isOnline);
}
