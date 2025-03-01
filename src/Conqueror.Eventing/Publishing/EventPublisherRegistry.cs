using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventPublisherRegistry(IEnumerable<EventPublisherRegistration> registrations)
{
    private readonly List<EventPublisherRegistration> registrations = registrations.ToList();
    private readonly ConcurrentDictionary<Type, List<(EventPublisherRegistration Registration, object Configuration)>> registrationsByEventType = new();

    public IReadOnlyCollection<(EventPublisherRegistration Registration, object Configuration)> GetRelevantPublishersForEventType<TEvent>()
        where TEvent : class
    {
        return registrationsByEventType.GetOrAdd(typeof(TEvent), GetRelevantPublishersForEventType);
    }

    private List<(EventPublisherRegistration Registration, object Configuration)> GetRelevantPublishersForEventType(Type eventType)
    {
        var result = new List<(EventPublisherRegistration Registration, object Configuration)>();

        foreach (var (registration, customAttribute) in from customAttribute in eventType.GetCustomAttributes()
                                                        where customAttribute is IConquerorEventTransportConfigurationAttribute
                                                        let registration = registrations.Find(r => r.ConfigurationAttributeType == customAttribute.GetType())
                                                        select (registration, customAttribute))
        {
            if (registration is null)
            {
                throw new ConquerorUnknownEventTransportPublisherException($"trying to publish event with unknown publisher for transport attribute type '{customAttribute.GetType().Name}'");
            }

            result.Add((registration, customAttribute));
        }

        if (result.Count == 0)
        {
            var registration = registrations.Find(r => r.ConfigurationAttributeType == typeof(InMemoryEventAttribute));

            if (registration is null)
            {
                // defensive programming; this code should never be reached
                throw new ConquerorUnknownEventTransportPublisherException("did not find Conqueror in-memory event publisher registration");
            }

            result.Add((registration, new InMemoryEventAttribute()));
        }

        return result;
    }
}

public sealed record EventPublisherRegistration(Type PublisherType, Type ConfigurationAttributeType, Action<IEventPublisherPipelineBuilder>? ConfigurePipeline);
