using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Chat.Server.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageType
{
    [EnumMember(Value = "message")]
    Message,
    
    [EnumMember(Value = "get_users")]
    GetUsers,
    
    [EnumMember(Value = "get_groups")]
    GetGroups,
    
    [EnumMember(Value = "create_group")]
    CreateGroup,
    
    [EnumMember(Value = "user_joined")]
    UserJoined,
    
    [EnumMember(Value = "user_left")]
    UserLeft,
    
    [EnumMember(Value = "users_list")]
    UsersList,
    
    [EnumMember(Value = "groups_list")]
    GroupsList,
    
    [EnumMember(Value = "group_created")]
    GroupCreated
}
