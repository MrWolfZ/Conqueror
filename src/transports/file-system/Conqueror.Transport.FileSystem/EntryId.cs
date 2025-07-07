namespace Conqueror.Transport.FileSystem;

internal readonly record struct EntryId
{
    public const byte IdLength = 16;

    private readonly string value;

    public EntryId(string value)
    {
        if (value.Length != IdLength)
        {
            throw new ArgumentException($"expected ID to be of length {IdLength}, but it was '{value}' with length {value.Length}", nameof(value));
        }

        this.value = value;
    }

    public static implicit operator string(EntryId id) => id.value;

    public override string ToString() => value;
}
