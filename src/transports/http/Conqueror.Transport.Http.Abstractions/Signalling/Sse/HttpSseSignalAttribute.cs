using System;
using Conqueror.Signalling;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     A signal transport that uses HTTP Server-Sent Events (SSE) to publish and receive signals.
/// </summary>
[SignalTransport(Prefix = "HttpSse", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpSseSignalAttribute : Attribute
{
    /// <summary>
    ///     The event type to use when publishing this signal type via an SSE connection.
    ///     Defaults to the camel-case type name of the signal type (stripped of the
    ///     <c>...Signal</c> suffix if any).
    /// </summary>
    public string? EventType { get; set; }
}
