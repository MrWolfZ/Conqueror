using System;

namespace Conqueror
{
    public sealed class EventObserverMetadata
    {
        public EventObserverMetadata(Type eventType, Type observerType, EventObserverOptions options)
        {
            EventType = eventType;
            ObserverType = observerType;
            Options = options;
        }

        public Type EventType { get; }

        public Type ObserverType { get; }

        public EventObserverOptions Options { get; }
    }
}
