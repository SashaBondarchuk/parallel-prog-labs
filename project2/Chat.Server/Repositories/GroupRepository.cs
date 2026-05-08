using System.Collections.Concurrent;
using Chat.Server.Models;

namespace Chat.Server.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly ConcurrentDictionary<string, Group> _groups = new();

    public Group? GetById(string id)
    {
        _groups.TryGetValue(id, out var group);
        return group;
    }

    public IEnumerable<Group> GetAll()
    {
        return _groups.Values.ToList();
    }

    public IEnumerable<Group> GetGroupsForUser(string userName)
    {
        return _groups.Values
            .Where(g => g.Members.Contains(userName))
            .ToList();
    }

    public void Add(Group group)
    {
        _groups.TryAdd(group.Id, group);
    }

    public bool Remove(string id)
    {
        return _groups.TryRemove(id, out _);
    }
}
