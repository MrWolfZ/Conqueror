using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

// NOTE: must be public for ASP Core to detect the attribute dynamically
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConquerorEventingWebSocketsTransportControllerRouteValueAttribute : RouteValueAttribute
{
    public ConquerorEventingWebSocketsTransportControllerRouteValueAttribute(string value)
        : base("controller", value)
    {
        Value = value;
    }

    public string Value { get; }
}
