using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventTypeRegistry : IConquerorEventTypeRegistry
{
    private readonly ConcurrentDictionary<(Type EventType, Type ConfigurationAttributeType), Attribute?> configurationByEventTypeAndConfigurationAttributeType = new();

    public bool TryGetConfigurationForReceiver<TConfigurationAttribute>(Type eventType, [NotNullWhen(true)] out TConfigurationAttribute? configurationAttribute)
        where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        configurationAttribute = configurationByEventTypeAndConfigurationAttributeType.GetOrAdd((eventType, typeof(TConfigurationAttribute)),
                                                                                                t => GetRelevancyOfEventType<TConfigurationAttribute>(t.EventType))
            as TConfigurationAttribute;

        return configurationAttribute is not null;
    }

    private static Attribute? GetRelevancyOfEventType<TConfigurationAttribute>(Type eventType)
        where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        var hasAnyConfigurationAttributes = eventType.GetCustomAttributes().OfType<IConquerorEventTransportConfigurationAttribute>().Any();
        var configurationAttribute = eventType.GetCustomAttribute<TConfigurationAttribute>();

        if (configurationAttribute is null && hasAnyConfigurationAttributes)
        {
            return null;
        }

        return configurationAttribute ?? (TConfigurationAttribute)(object)new InMemoryEventAttribute();
    }
}

internal sealed record EventObserverRegistration(
    Type EventType,
    Type ObserverType,
    Action<IEventObserverPipelineBuilder>? ConfigurePipeline);

internal sealed record EventObserverDelegateRegistration(
    Type EventType,
    Func<object, IServiceProvider, CancellationToken, Task> ObserverFn,
    Action<IEventObserverPipelineBuilder>? ConfigurePipeline);
