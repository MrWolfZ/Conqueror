using System;

namespace Conqueror.Signalling;

/// <summary>
///     This attribute type is used to annotate all Conqueror signal transport
///     marker attributes. It is used by source generators to find all signal
///     types and to determine the transports that each signal type belongs to.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SignalTransportAttribute : Attribute
{
    public required string Prefix { get; init; }

    /// <summary>
    ///     The namespace in which <b>ALL</b> types for this transport exist.
    /// </summary>
    public required string Namespace { get; init; }
}
