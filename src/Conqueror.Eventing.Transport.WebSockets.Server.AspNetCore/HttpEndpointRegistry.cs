using System;
using System.Collections.Generic;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

internal sealed class HttpEndpointRegistry
{
    private readonly ConquerorEventingWebSocketsTransportServerAspNetCoreOptions options;

    public HttpEndpointRegistry(ConquerorEventingWebSocketsTransportServerAspNetCoreOptions options)
    {
        this.options = options;
    }

    public IReadOnlyCollection<HttpEndpoint> GetEndpoints()
    {
        Console.WriteLine(options.EndpointPath);
        
        var endpoint = new HttpEndpoint
        {
            Path = options.EndpointPath ?? "api/events",
            Name = "Events",
            OperationId = "events",
            ControllerName = options.EndpointPath is null ? "Events" : $"EventsWithCustomPath`{options.EndpointPath.Replace("/", "_")}",
            ApiGroupName = "Events",
        };

        return new[] { endpoint };
    }
}
