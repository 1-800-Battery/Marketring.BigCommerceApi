namespace Fusionary.BigCommerce.Utils;

internal abstract class BcCartCollectionConverter : JsonConverter<List<object>>
{
    protected abstract JsonTokenType SingleTokenType { get; }

    // Retain the incoming scalar/object shape without changing the public List<object> contract.
    private sealed class SingleValueList : List<object>;

    public override List<object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<List<object>>(ref reader, options);
        }

        if (reader.TokenType == SingleTokenType)
        {
            return new SingleValueList { JsonSerializer.Deserialize<JsonElement>(ref reader, options) };
        }

        throw new JsonException("Unsupported cart collection shape.");
    }

    public override void Write(Utf8JsonWriter writer, List<object> value, JsonSerializerOptions options)
    {
        if (value is SingleValueList && value.Count == 1)
        {
            var single = JsonSerializer.SerializeToElement(value[0], options);
            if (single.ValueKind == JsonValueKind.Number && SingleTokenType == JsonTokenType.Number ||
                single.ValueKind == JsonValueKind.Object && SingleTokenType == JsonTokenType.StartObject)
            {
                single.WriteTo(writer);
                return;
            }
        }

        JsonSerializer.Serialize(writer, value, options);
    }
}

internal sealed class BcCartCouponsConverter : BcCartCollectionConverter
{
    protected override JsonTokenType SingleTokenType => JsonTokenType.Number;
}

internal sealed class BcCartBannersConverter : BcCartCollectionConverter
{
    protected override JsonTokenType SingleTokenType => JsonTokenType.StartObject;
}
