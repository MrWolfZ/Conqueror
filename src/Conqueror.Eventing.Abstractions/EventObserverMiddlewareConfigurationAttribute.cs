using System;

namespace Conqueror.Eventing
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class EventObserverMiddlewareConfigurationAttribute : Attribute
    {
    }
}
