namespace Chat.Server.Models;

public class Group
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Members { get; set; } = new();
}
