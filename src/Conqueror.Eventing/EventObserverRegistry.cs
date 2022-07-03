using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverRegistry
    {
        private readonly IReadOnlyDictionary<Type, IReadOnlyCollection<EventObserverMetadata>> metadataLookup;

        public EventObserverRegistry(IEnumerable<EventObserverMetadata> metadata)
        {
            metadataLookup = metadata.GroupBy(m => m.EventType).ToDictionary(g => g.Key, g => g.ToList() as IReadOnlyCollection<EventObserverMetadata>);
        }

        public IReadOnlyCollection<EventObserverMetadata> GetEventObserversMetadata<TEvent>()
            where TEvent : class
        {
            return metadataLookup.Keys
                                 .Where(t => typeof(TEvent).IsAssignableTo(t))
                                 .SelectMany(t => metadataLookup[t])
                                 .ToList();
        }
    }
}
