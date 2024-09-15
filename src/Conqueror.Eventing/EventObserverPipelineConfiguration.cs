using System;

namespace Conqueror.Eventing;

internal sealed class EventObserverPipelineConfiguration(Type observerType, Action<IEventObserverPipelineBuilder> configure)
{
    public Type ObserverType { get; } = observerType;

    public Action<IEventObserverPipelineBuilder> Configure { get; } = configure;
}
