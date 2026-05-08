namespace Chat.Server.Models;

public class User
{
    public string Name { get; set; } = string.Empty;
    public bool Online { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is User user && Name == user.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
