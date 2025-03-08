using System;

namespace Conqueror;

public abstract class EventTransportAttribute(string transportTypeName) : Attribute
{
    public string TransportTypeName { get; } = transportTypeName;
}
