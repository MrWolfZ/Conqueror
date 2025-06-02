using System;
using Conqueror.Signalling;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     A signal transport that uses HTTP web sockets to publish and receive signals.
/// </summary>
[SignalTransport(Prefix = "HttpWebSockets", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpWebSocketsSignalAttribute : Attribute
{
    /// <summary>
    ///     The tag to identify signals of this type when publishing via websockets.
    ///     Defaults to the camel-case type name of the signal type (stripped of the
    ///     <c>...Signal</c> suffix if any).
    /// </summary>
    public string? Tag { get; set; }
}
