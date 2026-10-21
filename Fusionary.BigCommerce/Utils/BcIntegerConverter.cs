using System.Buffers;
using System.Globalization;
using System.Numerics;

namespace Fusionary.BigCommerce.Utils;

/// <summary>Reads whole-valued JSON numbers without rounding quantities or identifiers.</summary>
internal sealed class BcIntegerConverter<T> : JsonConverter<T> where T : struct, IBinaryInteger<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        T value;
        if (reader.TokenType == JsonTokenType.Number &&
            T.TryParse(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
            reader.TokenType == JsonTokenType.String &&
            T.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        throw new JsonException("Expected a whole number within the supported range.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(decimal.CreateChecked(value));
}
