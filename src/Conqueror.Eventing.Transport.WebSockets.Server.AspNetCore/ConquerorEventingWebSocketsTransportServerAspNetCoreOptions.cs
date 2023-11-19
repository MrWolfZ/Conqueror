namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

public sealed class ConquerorEventingWebSocketsTransportServerAspNetCoreOptions
{
    /// <summary>
    /// The path under which the events are being served.
    /// </summary>
    public string? EndpointPath { get; set; }
}
