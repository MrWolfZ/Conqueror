using System;
using System.Collections.Generic;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventBroadcastingStrategyBuilder(IServiceProvider serviceProvider) : IConquerorEventBroadcastingStrategyBuilder
{
    private readonly Dictionary<Type, IConquerorEventBroadcastingStrategy> strategyByEventType = new();

    private IConquerorEventBroadcastingStrategy defaultStrategy = new SequentialBroadcastingStrategy(new());

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IConquerorEventBroadcastingStrategyBuilder UseDefault(IConquerorEventBroadcastingStrategy strategy)
    {
        defaultStrategy = strategy;
        return this;
    }

    public IConquerorEventBroadcastingStrategyBuilder UseForEventType<TEvent>(IConquerorEventBroadcastingStrategy strategy)
        where TEvent : class
    {
        strategyByEventType[typeof(TEvent)] = strategy;
        return this;
    }

    public ConfiguredEventBroadcastingStrategy Build(IReadOnlyCollection<EventObserverRegistration> observerRegistrations)
    {
        return new(defaultStrategy, strategyByEventType, observerRegistrations);
    }
}
