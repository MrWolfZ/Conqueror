using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventPublisherRegistry(IEnumerable<EventTransportPublisherRegistration> registrations)
{
    private readonly List<EventTransportPublisherRegistration> registrations = registrations.ToList();
    private readonly ConcurrentDictionary<Type, List<(EventTransportPublisherRegistration Registration, EventTransportAttribute Configuration)>> registrationsByEventType = new();

    public IReadOnlyCollection<(EventTransportPublisherRegistration Registration, EventTransportAttribute Configuration)> GetRelevantPublishersForEventType(Type eventType)
    {
        return registrationsByEventType.GetOrAdd(eventType, CreateRelevantPublishersForEventType);
    }

    private List<(EventTransportPublisherRegistration Registration, EventTransportAttribute Configuration)> CreateRelevantPublishersForEventType(Type eventType)
    {
        var result = (from customAttribute in eventType.GetCustomAttributes()
                      where customAttribute is EventTransportAttribute
                      let registrations = registrations.Where(r => r.ConfigurationAttributeType == customAttribute.GetType()).ToList()
                      select (registrations, (EventTransportAttribute)customAttribute)).ToList();

        var validResults = result.Where(r => r.registrations.Count > 0).SelectMany(t => t.registrations.Select(r => (r, t.Item2))).ToList();

        if (validResults.Count == 0 && result.Count > 0)
        {
            throw new UnregisteredEventTransportPublisherException($"trying to publish event with unknown publisher for transport attribute type '{result[0].Item2.GetType().Name}'");
        }

        if (validResults.Count == 0)
        {
            var registration = registrations.Find(r => r.ConfigurationAttributeType == typeof(InProcessEventAttribute));

            if (registration is null)
            {
                // defensive programming; this code should never be reached
                throw new UnregisteredEventTransportPublisherException("did not find Conqueror in-memory event publisher registration");
            }

            validResults.Add((registration, new InProcessEventAttribute()));
        }

        return validResults;
    }
}

internal sealed record EventTransportPublisherRegistration(Type PublisherType, Type ConfigurationAttributeType);
