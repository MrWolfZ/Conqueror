using System;
using System.Collections.Generic;

namespace Conqueror.Eventing;

internal sealed class InMemoryEventPublishingStrategyBuilder(IServiceProvider serviceProvider) : IConquerorInMemoryEventPublishingStrategyBuilder
{
    private readonly Dictionary<Type, IConquerorInMemoryEventPublishingStrategy> strategyByEventType = new();

    private IConquerorInMemoryEventPublishingStrategy defaultStrategy = new InMemorySequentialPublishingStrategy(new());

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

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
