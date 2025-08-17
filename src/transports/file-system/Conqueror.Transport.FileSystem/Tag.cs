namespace Conqueror.Transport.FileSystem;

[JsonConverter(typeof(TagJsonConverter))]
internal readonly record struct Tag
{
    private readonly string value;

    public Tag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"expected tag to be non-null, non-whitespace string, but it was '{value}'",
                nameof(value)
            );
        }

        this.value = value;
    }

    public static implicit operator string(Tag tag) => tag.value;

    public override string ToString() => value;
}

internal sealed class TagJsonConverter : JsonConverter<Tag>
{
    public override Tag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, Tag value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);

    public override Tag ReadAsPropertyName(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) => new(reader.GetString()!);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Tag value, JsonSerializerOptions options) =>
        writer.WritePropertyName(value);
}
