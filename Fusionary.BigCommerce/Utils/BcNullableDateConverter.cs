namespace Fusionary.BigCommerce.Utils;

internal sealed class BcNullableDateConverter<T> : JsonConverter<T?> where T : struct
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ||
        reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString())
            ? null
            : ((JsonConverter<T>)options.GetConverter(typeof(T))).Read(ref reader, typeof(T), options);

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            ((JsonConverter<T>)options.GetConverter(typeof(T))).Write(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
