using System;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverMiddlewareMetadata
    {
        public EventObserverMiddlewareMetadata(Type middlewareType, Type attributeType)
        {
            MiddlewareType = middlewareType;
            AttributeType = attributeType;
        }

        public Type MiddlewareType { get; }

        public Type AttributeType { get; }
    }
}
