using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreeDGod.Core.Domain;

public static class DomainJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json)
    {
        var result = JsonSerializer.Deserialize<T>(json, Options);
        if (result is null)
            throw new InvalidOperationException($"Domain JSON deserialized to null for {typeof(T).Name}.");
        return result;
    }

    public static T Roundtrip<T>(T value) => Deserialize<T>(Serialize(value));

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
