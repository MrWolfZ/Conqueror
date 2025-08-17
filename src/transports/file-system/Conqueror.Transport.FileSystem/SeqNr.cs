namespace Conqueror.Transport.FileSystem;

internal readonly record struct SeqNr
{
    private readonly ulong value;

    public SeqNr(ulong value) => this.value = value;

    public static implicit operator ulong(SeqNr seqNr) => seqNr.value;

    public string ToPaddedString(byte length) => value.ToString($"D{length}", CultureInfo.InvariantCulture);

    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);
}
