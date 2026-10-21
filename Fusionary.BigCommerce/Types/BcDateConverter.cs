namespace Fusionary.BigCommerce.Types;

public class BcDateConverter : JsonConverter<BcDate>
{
    public override bool HandleNull => true;

    public override BcDate Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        reader switch
        {
            { TokenType: JsonTokenType.Null } => default,
            { TokenType: JsonTokenType.String } => ReadDate(reader.GetString()),
            { TokenType: JsonTokenType.Number } => ReadTimestamp(ref reader),
            _ => throw new JsonException()
        };

    private static BcDate ReadTimestamp(ref Utf8JsonReader reader)
    {
        if (reader.TryGetInt64(out var seconds) && seconds >= -62135596800 && seconds <= 253402300799)
        {
            return new BcDate(DateTimeOffset.FromUnixTimeSeconds(seconds));
        }

        throw new JsonException("Timestamp is outside the supported range.");
    }

    private static BcDate ReadDate(string? value)
    {
        var date = new BcDate(value);
        if (!string.IsNullOrWhiteSpace(value) && !date.HasValue)
        {
            throw new JsonException("Invalid date.");
        }

        return date;
    }

    public override void Write(
        Utf8JsonWriter writer,
        BcDate value,
        JsonSerializerOptions options
    )
    {
        if (value.HasValue )
        {
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}