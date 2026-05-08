using System.Collections.Concurrent;
using Chat.Server.Models;

namespace Chat.Server.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new();

    public User? GetByName(string name)
    {
        _users.TryGetValue(name, out var user);
        return user;
    }

    public IEnumerable<User> GetAll()
    {
        return _users.Values.ToList();
    }

    public IEnumerable<User> GetOnlineUsers()
    {
        return _users.Values.Where(u => u.Online).ToList();
    }

    public void AddOrUpdate(User user)
    {
        _users.AddOrUpdate(user.Name, user, (_, _) => user);
    }

    public bool SetOnlineStatus(string userName, bool isOnline)
    {
        if (_users.TryGetValue(userName, out var user))
        {
            user.Online = isOnline;
            return true;
        }
        return false;
    }
}
