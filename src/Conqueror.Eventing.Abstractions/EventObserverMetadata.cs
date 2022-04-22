using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMetadata
    {
        public EventObserverMetadata(Type eventType, Type observerType, EventObserverOptions options)
        {
            EventType = eventType;
            ObserverType = observerType;
            Options = options;
            MiddlewareConfigurationAttributes = GetConfigurationAttribute(observerType).ToList();
        }

        public Type EventType { get; }

        public Type ObserverType { get; }

        public EventObserverOptions Options { get; }

        public IReadOnlyCollection<EventObserverMiddlewareConfigurationAttribute> MiddlewareConfigurationAttributes { get; }

        private IEnumerable<EventObserverMiddlewareConfigurationAttribute> GetConfigurationAttribute(Type observerType)
        {
            var executeMethod = observerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                                            .Where(m => m.Name == nameof(IEventObserver<object>.HandleEvent))
                                            .First(m => m.GetParameters().First().ParameterType == EventType);

            return executeMethod.GetCustomAttributes().OfType<EventObserverMiddlewareConfigurationAttribute>();
        }
    }
}
