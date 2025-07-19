namespace Conqueror.Transport.FileSystem;

internal readonly record struct InboxName
{
    private readonly string value;

    public InboxName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"expected inbox name to be non-null, non-whitespace string, but it was '{value}'", nameof(value));
        }

        this.value = value;
    }

    public static implicit operator string(InboxName inboxName) => inboxName.value;

    public override string ToString() => value;
}
