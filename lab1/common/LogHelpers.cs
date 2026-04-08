namespace common;

public class LogHelpers
{
    public static void LogListInOneLine<T>(IList<T> list)
    {
        string values = string.Join(", ", list);
        Console.WriteLine(values);
    }
}