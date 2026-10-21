namespace Fusionary.BigCommerce.Types;

public class BcIdConverter : JsonConverter<BcId>
{
    public override BcId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => new BcId(new BcIntegerConverter<int>().Read(ref reader, typeof(int), options));

    public override void Write(
        Utf8JsonWriter writer,
        BcId value,
        JsonSerializerOptions options
    ) =>
        writer.WriteNumberValue(value.Value);
}