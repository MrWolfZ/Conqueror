using System;

namespace Conqueror;

[AttributeUsage(AttributeTargets.Class)]
public sealed class WebSocketsEventAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
{
    /// <summary>
    ///     The ID of this event type. Clients use this ID to specify the events they are interested in.
    ///     Defaults to <see cref="Type.FullName" /> of the event type.
    /// </summary>
    public string? EventTypeId { get; set; }
}
