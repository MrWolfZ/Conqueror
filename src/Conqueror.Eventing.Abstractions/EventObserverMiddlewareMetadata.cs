using System;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMiddlewareMetadata
    {
        public EventObserverMiddlewareMetadata(Type middlewareType, Type? configurationType)
        {
            MiddlewareType = middlewareType;
            ConfigurationType = configurationType;
        }

        public Type MiddlewareType { get; }
        
        public Type? ConfigurationType { get; }
    }
}
