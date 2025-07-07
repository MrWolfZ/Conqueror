namespace Conqueror.Transport.FileSystem;

internal readonly record struct Tag
{
    private readonly string value;

    public Tag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"expected tag to be non-null, non-whitespace string, but it was {value.Length}", nameof(value));
        }

        this.value = value;
    }

    public static implicit operator string(Tag tag) => tag.value;

    public override string ToString() => value;
}
