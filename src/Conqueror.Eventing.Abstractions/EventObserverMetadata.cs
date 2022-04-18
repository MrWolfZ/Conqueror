using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            MiddlewareConfigurationAttributes = GetConfigurationAttribute(observerType).ToDictionary(t => (t.EventType, t.Attribute.GetType()), t => t.Attribute);
        }

        public Type EventType { get; }

        public Type ObserverType { get; }

        public EventObserverOptions Options { get; }

        public IReadOnlyDictionary<(Type EventType, Type AttributeType), EventObserverMiddlewareConfigurationAttribute> MiddlewareConfigurationAttributes { get; }

        public bool TryGetMiddlewareConfiguration<TEvent, TConfiguration>([MaybeNullWhen(false)] out TConfiguration attribute)
            where TEvent : class
            where TConfiguration : EventObserverMiddlewareConfigurationAttribute, IEventObserverMiddlewareConfiguration<IEventObserverMiddleware<TConfiguration>>
        {
            var success = MiddlewareConfigurationAttributes.TryGetValue((typeof(TEvent), typeof(TConfiguration)), out var a);
            attribute = a as TConfiguration;
            return success && attribute != null;
        }

        private static IEnumerable<(Type EventType, EventObserverMiddlewareConfigurationAttribute Attribute)> GetConfigurationAttribute(Type observerType)
        {
            var executeMethods = observerType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(m => m.Name == nameof(IEventObserver<object>.HandleEvent));
            return executeMethods.SelectMany(m => m.GetCustomAttributes().OfType<EventObserverMiddlewareConfigurationAttribute>().Select(a => (m.GetParameters().First().ParameterType, a)));
        }
    }
}
