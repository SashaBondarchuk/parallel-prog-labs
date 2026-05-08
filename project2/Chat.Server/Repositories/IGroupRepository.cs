using Chat.Server.Models;

namespace Chat.Server.Repositories;

public interface IGroupRepository
{
    Group? GetById(string id);
    IEnumerable<Group> GetAll();
    IEnumerable<Group> GetGroupsForUser(string userName);
    void Add(Group group);
    bool Remove(string id);
}
