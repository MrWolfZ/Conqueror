using System;

namespace Conqueror;

public interface IConquerorEventBroadcastingStrategyBuilder
{
    // TODO: add test for this
    IServiceProvider ServiceProvider { get; }

    IConquerorEventBroadcastingStrategyBuilder UseDefault(IConquerorEventBroadcastingStrategy strategy);

    IConquerorEventBroadcastingStrategyBuilder UseForEventType<TEvent>(IConquerorEventBroadcastingStrategy strategy)
        where TEvent : class;
}
