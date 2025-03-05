using System;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

public interface IConquerorEventTypeRegistry
{
    bool TryGetConfigurationForReceiver<TConfigurationAttribute>(Type eventType, [NotNullWhen(true)] out TConfigurationAttribute? configurationAttribute)
        where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;
}
