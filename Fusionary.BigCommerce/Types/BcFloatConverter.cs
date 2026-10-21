using System.Globalization;

namespace Fusionary.BigCommerce.Types;

public class BcFloatConverter : JsonConverter<BcFloat>
{
    public override BcFloat Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        reader switch
        {
            { TokenType: JsonTokenType.Number } => new BcFloat(reader.GetDecimal()),
            { TokenType: JsonTokenType.String } when decimal.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => new BcFloat(value),
            _ => throw new JsonException()
        };

    public override void Write(
        Utf8JsonWriter writer,
        BcFloat value,
        JsonSerializerOptions options
    ) => writer.WriteNumberValue(value.Value);
}