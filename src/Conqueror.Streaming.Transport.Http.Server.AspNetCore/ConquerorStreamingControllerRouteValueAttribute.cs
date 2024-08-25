using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

// NOTE: must be public for ASP Core to detect the attribute dynamically
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConquerorStreamingControllerRouteValueAttribute : RouteValueAttribute
{
    public ConquerorStreamingControllerRouteValueAttribute(string value)
        : base("controller", value)
    {
        Value = value;
    }

    public string Value { get; }
}