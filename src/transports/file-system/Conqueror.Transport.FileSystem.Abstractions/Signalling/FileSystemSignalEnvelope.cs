namespace Conqueror.Signalling;

internal sealed record FileSystemSignalEnvelope(object Signal, string? ContextData)
{
    public const string Discriminator = "envelope";
}
