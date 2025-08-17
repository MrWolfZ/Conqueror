namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

using Microsoft.AspNetCore.Mvc.Routing;

// NOTE: must be public for ASP Core to detect the attribute dynamically
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConquerorStreamingControllerRouteValueAttribute(string value)
    : RouteValueAttribute("controller", value)
{
    public string Value { get; } = value;
}
