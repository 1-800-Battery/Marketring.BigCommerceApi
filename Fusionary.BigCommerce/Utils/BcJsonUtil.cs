using System.Net.Http.Json;

namespace Fusionary.BigCommerce.Utils;

public static class BcJsonUtil
{
    public static readonly BcSnakeCaseNamingPolicy SnakeCaseNamingPolicy = new();

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = SnakeCaseNamingPolicy,
        DictionaryKeyPolicy = SnakeCaseNamingPolicy,
        Converters =
        {
            new BcResponseEnumConverter(),
            new BcIntegerConverter<int>(),
            new BcIntegerConverter<long>(),
            new BcNullableDateConverter<BcDateTime>(),
            new BcNullableDateConverter<BcDate>()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static JsonContent CreateContent(object? payload) => JsonContent.Create(payload, options: JsonOptions);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);

    public static string Serialize<T>(T value, bool writeIndented = false) =>
        JsonSerializer.Serialize(
            value,
            writeIndented
                ? new JsonSerializerOptions(JsonOptions) { WriteIndented = writeIndented }
                : JsonOptions
        );
}