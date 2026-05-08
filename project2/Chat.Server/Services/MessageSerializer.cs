using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Chat.Server.Enums;

namespace Chat.Server.Services;

public class MessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public MessageSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        
        _options.Converters.Add(new SnakeCaseEnumConverter<MessageType>());
    }

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}

public partial class SnakeCaseEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException();

        foreach (var enumValue in Enum.GetValues<T>())
        {
            if (ToSnakeCase(enumValue.ToString()) == value)
                return enumValue;
        }

        throw new JsonException($"Cannot convert '{value}' to {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToSnakeCase(value.ToString()));
    }

    private static string ToSnakeCase(string input)
    {
        return MyRegex().Replace(input, "_$1")
            .ToLower();
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex MyRegex();
}
