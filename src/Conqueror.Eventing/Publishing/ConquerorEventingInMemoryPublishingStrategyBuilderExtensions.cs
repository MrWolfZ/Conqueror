using System;
using Conqueror;
using Conqueror.Eventing.Publishing;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingInMemoryPublishingStrategyBuilderExtensions
{
    public static IConquerorInMemoryEventPublishingStrategyBuilder UseSequentialAsDefault(this IConquerorInMemoryEventPublishingStrategyBuilder builder,
                                                                                          Action<SequentialInMemoryEventPublishingStrategyConfiguration>? configure = null)
    {
        var configuration = new SequentialInMemoryEventPublishingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseDefault(new InMemorySequentialPublishingStrategy(configuration));
    }

    public static IConquerorInMemoryEventPublishingStrategyBuilder UseSequentialForEventType<TEvent>(this IConquerorInMemoryEventPublishingStrategyBuilder builder,
                                                                                                     Action<SequentialInMemoryEventPublishingStrategyConfiguration>? configure = null)
        where TEvent : class
    {
        var configuration = new SequentialInMemoryEventPublishingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseForEventType<TEvent>(new InMemorySequentialPublishingStrategy(configuration));
    }

    public static IConquerorInMemoryEventPublishingStrategyBuilder UseParallelAsDefault(this IConquerorInMemoryEventPublishingStrategyBuilder builder,
                                                                                        Action<ParallelInMemoryEventPublishingStrategyConfiguration>? configure = null)
    {
        var configuration = new ParallelInMemoryEventPublishingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseDefault(new InMemoryParallelPublishingStrategy(configuration));
    }

    public static IConquerorInMemoryEventPublishingStrategyBuilder UseParallelForEventType<TEvent>(this IConquerorInMemoryEventPublishingStrategyBuilder builder,
                                                                                                   Action<ParallelInMemoryEventPublishingStrategyConfiguration>? configure = null)
        where TEvent : class
    {
        var configuration = new ParallelInMemoryEventPublishingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseForEventType<TEvent>(new InMemoryParallelPublishingStrategy(configuration));
    }
}
