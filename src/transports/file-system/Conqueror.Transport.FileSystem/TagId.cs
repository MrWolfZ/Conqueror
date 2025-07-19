namespace Conqueror.Transport.FileSystem;

[JsonConverter(typeof(TagIdJsonConverter))]
internal readonly record struct TagId
{
    private readonly uint value;

    public TagId(uint value)
    {
        this.value = value;
    }

    public string ToPaddedString(byte length) => value.ToString($"D{length}");

    public override string ToString() => value.ToString();

    public static implicit operator uint(TagId tagId) => tagId.value;
}

internal sealed class TagIdJsonConverter : JsonConverter<TagId>
{
    public override TagId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetUInt32());

    public override void Write(Utf8JsonWriter writer, TagId value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
