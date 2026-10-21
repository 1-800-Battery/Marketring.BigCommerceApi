namespace Fusionary.BigCommerce.Utils;

/// <summary>Preserves known enum wire names and maps future string values to an undefined sentinel.</summary>
internal sealed class BcResponseEnumConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(typeToConvert), options)!;

    private sealed class EnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly JsonConverter<T> _converter;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "S1144", Justification = "Constructed by the converter factory through reflection.")]
        public EnumConverter(JsonSerializerOptions options)
        {
            _converter = (JsonConverter<T>)new JsonStringEnumMemberConverter(BcJsonUtil.SnakeCaseNamingPolicy)
                .CreateConverter(typeof(T), options);
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return _converter.Read(ref reader, typeToConvert, options);
            }
            catch (JsonException) when (reader.TokenType == JsonTokenType.String)
            {
                // All SDK enums use signed Int32; -1 cannot be mistaken for their zero-valued defaults.
                return (T)Enum.ToObject(typeof(T), -1);
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            _converter.Write(writer, value, options);
    }
}
