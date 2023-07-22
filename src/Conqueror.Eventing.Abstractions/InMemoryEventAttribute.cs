using System;

namespace Conqueror;

[AttributeUsage(AttributeTargets.Class)]
public sealed class InMemoryEventAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
{
}
