using System;
using Conqueror;
using Conqueror.Eventing.Publishing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingBroadcastStrategyServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services,
                                                                                      ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TStrategy : IConquerorEventBroadcastingStrategy
    {
        return services.Replace(new(typeof(IConquerorEventBroadcastingStrategy), typeof(TStrategy), lifetime));
    }

    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services,
                                                                                      Func<IServiceProvider, TStrategy> factory,
                                                                                      ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TStrategy : IConquerorEventBroadcastingStrategy
    {
        return services.Replace(new(typeof(IConquerorEventBroadcastingStrategy), p => factory(p), lifetime));
    }

    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services,
                                                                                      TStrategy instance)
        where TStrategy : IConquerorEventBroadcastingStrategy
    {
        return services.Replace(new(typeof(IConquerorEventBroadcastingStrategy), instance));
    }

    public static IServiceCollection AddSequentialConquerorEventBroadcastingStrategy(this IServiceCollection services,
                                                                                     Action<SequentialEventBroadcastingStrategyConfiguration>? configure = null)
    {
        var configuration = new SequentialEventBroadcastingStrategyConfiguration();
        configure?.Invoke(configuration);
        return services.AddConquerorEventBroadcastingStrategy(new SequentialBroadcastingStrategy(configuration));
    }

    public static IServiceCollection AddParallelConquerorEventBroadcastingStrategy(this IServiceCollection services,
                                                                                   Action<ParallelEventBroadcastingStrategyConfiguration>? configure = null)
    {
        var configuration = new ParallelEventBroadcastingStrategyConfiguration();
        configure?.Invoke(configuration);
        return services.AddConquerorEventBroadcastingStrategy(new ParallelBroadcastingStrategy(configuration));
    }
}
