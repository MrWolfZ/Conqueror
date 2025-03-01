using System;
using Conqueror;
using Conqueror.Eventing.Publishing;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingBroadcastingStrategyBuilderExtensions
{
    public static IConquerorEventBroadcastingStrategyBuilder UseSequentialAsDefault(this IConquerorEventBroadcastingStrategyBuilder builder,
                                                                                    Action<SequentialEventBroadcastingStrategyConfiguration>? configure = null)
    {
        var configuration = new SequentialEventBroadcastingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseDefault(new SequentialBroadcastingStrategy(configuration));
    }

    public static IConquerorEventBroadcastingStrategyBuilder UseSequentialForEventType<TEvent>(this IConquerorEventBroadcastingStrategyBuilder builder,
                                                                                               Action<SequentialEventBroadcastingStrategyConfiguration>? configure = null)
        where TEvent : class
    {
        var configuration = new SequentialEventBroadcastingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseForEventType<TEvent>(new SequentialBroadcastingStrategy(configuration));
    }

    public static IConquerorEventBroadcastingStrategyBuilder UseParallelAsDefault(this IConquerorEventBroadcastingStrategyBuilder builder,
                                                                                  Action<ParallelEventBroadcastingStrategyConfiguration>? configure = null)
    {
        var configuration = new ParallelEventBroadcastingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseDefault(new ParallelBroadcastingStrategy(configuration));
    }

    public static IConquerorEventBroadcastingStrategyBuilder UseParallelForEventType<TEvent>(this IConquerorEventBroadcastingStrategyBuilder builder,
                                                                                             Action<ParallelEventBroadcastingStrategyConfiguration>? configure = null)
        where TEvent : class
    {
        var configuration = new ParallelEventBroadcastingStrategyConfiguration();
        configure?.Invoke(configuration);
        return builder.UseForEventType<TEvent>(new ParallelBroadcastingStrategy(configuration));
    }
}
