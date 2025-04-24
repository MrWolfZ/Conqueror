using System;

namespace Conqueror.Messaging;

/// <summary>
///     This attribute type is used to annotate all Conqueror message transport
///     marker attributes. It is used by source generators to find all message
///     types and to determine the transports that each message type belongs to.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MessageTransportAttribute : Attribute
{
    public required string Prefix { get; init; }

    /// <summary>
    ///     The namespace in which <b>ALL</b> types for this transport exist.
    /// </summary>
    public required string Namespace { get; init; }
}
