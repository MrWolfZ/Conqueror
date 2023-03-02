using System;

namespace Conqueror.Eventing;

internal sealed class EventObserverPipelineConfiguration
{
    public EventObserverPipelineConfiguration(Type observerType, Action<IEventObserverPipelineBuilder> configure)
    {
        ObserverType = observerType;
        Configure = configure;
    }

    public Type ObserverType { get; }

    public Action<IEventObserverPipelineBuilder> Configure { get; }
}
