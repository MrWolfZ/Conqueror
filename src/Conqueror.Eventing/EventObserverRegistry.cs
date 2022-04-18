using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverRegistry
    {
        private readonly IReadOnlyDictionary<Type, IReadOnlyCollection<EventObserverMetadata>> metadataLookup;

        public EventObserverRegistry(IEnumerable<EventObserverMetadata> metadata)
        {
            metadataLookup = metadata.GroupBy(m => m.EventType).ToDictionary(g => g.Key, g => g.ToList() as IReadOnlyCollection<EventObserverMetadata>);
        }

        public IReadOnlyCollection<(IEventObserver<TEvent> Observer, EventObserverMetadata Metadata)> GetEventObservers<TEvent>(IServiceProvider serviceProvider)
            where TEvent : class
        {
            return metadataLookup.Keys
                                 .Where(t => typeof(TEvent).IsAssignableTo(t))
                                 .SelectMany(t => metadataLookup[t])
                                 .Select(m => ((IEventObserver<TEvent>)serviceProvider.GetRequiredService(m.ObserverType), m))
                                 .ToList();
        }
    }
}
