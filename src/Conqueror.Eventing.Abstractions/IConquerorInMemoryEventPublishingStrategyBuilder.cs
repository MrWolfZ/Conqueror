using System;

namespace Conqueror;

public interface IConquerorInMemoryEventPublishingStrategyBuilder
{
    // TODO: add test for this
    IServiceProvider ServiceProvider { get; }

    IConquerorInMemoryEventPublishingStrategyBuilder UseDefault(IConquerorInMemoryEventPublishingStrategy strategy);

    IConquerorInMemoryEventPublishingStrategyBuilder UseForEventType<TEvent>(IConquerorInMemoryEventPublishingStrategy strategy)
        where TEvent : class;
}
