namespace Conqueror.Signalling.WebSockets;

internal sealed record HttpWebSocketsSignalEnvelope(object Signal, string? ContextData)
{
    public const string Discriminator = "envelope";
}
