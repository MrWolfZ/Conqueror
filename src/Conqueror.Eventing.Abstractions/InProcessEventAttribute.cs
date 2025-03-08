using System;

namespace Conqueror;

[AttributeUsage(AttributeTargets.Class)]
public sealed class InProcessEventAttribute() : EventTransportAttribute(TransportName)
{
    public const string TransportName = "in-process";
}
