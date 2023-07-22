using System;
using System.Collections.Generic;

namespace Conqueror.Eventing;

internal sealed class InMemoryEventPublishingStrategyBuilder : IConquerorInMemoryEventPublishingStrategyBuilder
{
    private readonly Dictionary<Type, IConquerorInMemoryEventPublishingStrategy> strategyByEventType = new();

    private IConquerorInMemoryEventPublishingStrategy defaultStrategy = new InMemorySequentialPublishingStrategy(new());

    public InMemoryEventPublishingStrategyBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public IConquerorInMemoryEventPublishingStrategyBuilder UseDefault(IConquerorInMemoryEventPublishingStrategy strategy)
    {
        defaultStrategy = strategy;
        return this;
    }

    public IConquerorInMemoryEventPublishingStrategyBuilder UseForEventType<TEvent>(IConquerorInMemoryEventPublishingStrategy strategy)
        where TEvent : class
    {
        strategyByEventType[typeof(TEvent)] = strategy;
        return this;
    }

    public InMemoryEventPublishingConfiguredStrategy Build(IReadOnlyCollection<EventObserverRegistration> observerRegistrations)
    {
        return new(defaultStrategy, strategyByEventType, observerRegistrations);
    }
}
