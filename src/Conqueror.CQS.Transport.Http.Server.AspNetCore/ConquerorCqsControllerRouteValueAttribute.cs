using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

// NOTE: must be public for ASP Core to detect the attribute dynamically
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConquerorCqsControllerRouteValueAttribute(string value) : RouteValueAttribute("controller", value)
{
    public string Value { get; } = value;
}
