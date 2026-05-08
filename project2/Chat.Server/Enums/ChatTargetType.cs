using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Chat.Server.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatTargetType
{
    [EnumMember(Value = "user")]
    User,
    
    [EnumMember(Value = "group")]
    Group
}
