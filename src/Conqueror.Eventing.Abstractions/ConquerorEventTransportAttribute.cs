using System;

namespace Conqueror;

public abstract class ConquerorEventTransportAttribute(string transportTypeName) : Attribute
{
    public string TransportTypeName { get; } = transportTypeName;
}
