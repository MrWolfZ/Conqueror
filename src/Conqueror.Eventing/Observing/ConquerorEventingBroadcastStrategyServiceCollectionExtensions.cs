using System;
using Conqueror;
using Conqueror.Eventing.Publishing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingBroadcastStrategyServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services)
        where TStrategy : IEventBroadcastingStrategy
    {
        return services.AddConquerorEventBroadcastingStrategy<TStrategy>(ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services, ServiceLifetime lifetime)
        where TStrategy : IEventBroadcastingStrategy
    {
        services.Replace(new(typeof(TStrategy), typeof(TStrategy), lifetime));
        return services.Replace(new(typeof(IEventBroadcastingStrategy), typeof(TStrategy), lifetime));
    }

    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services,
                                                                                      Func<IServiceProvider, TStrategy> factory)
        where TStrategy : IEventBroadcastingStrategy
    {
        return services.AddConquerorEventBroadcastingStrategy(factory, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services,
                                                                                      Func<IServiceProvider, TStrategy> factory,
                                                                                      ServiceLifetime lifetime)
        where TStrategy : IEventBroadcastingStrategy
    {
        services.Replace(new(typeof(TStrategy), p => factory(p), lifetime));
        return services.Replace(new(typeof(IEventBroadcastingStrategy), p => factory(p), lifetime));
    }

    public static IServiceCollection AddConquerorEventBroadcastingStrategy<TStrategy>(this IServiceCollection services, TStrategy instance)
        where TStrategy : IEventBroadcastingStrategy
    {
        services.Replace(new(typeof(TStrategy), instance));
        return services.Replace(new(typeof(IEventBroadcastingStrategy), instance));
    }

    public static IServiceCollection AddSequentialConquerorEventBroadcastingStrategy(this IServiceCollection services)
    {
        return services.AddConquerorEventBroadcastingStrategy(new SequentialBroadcastingStrategy(new()));
    }

    public static IServiceCollection AddSequentialConquerorEventBroadcastingStrategy(this IServiceCollection services,
                                                                                     Action<SequentialEventBroadcastingStrategyConfiguration> configure)
    {
        var configuration = new SequentialEventBroadcastingStrategyConfiguration();
        configure.Invoke(configuration);
        return services.AddConquerorEventBroadcastingStrategy(new SequentialBroadcastingStrategy(configuration));
    }

    public static IServiceCollection AddParallelConquerorEventBroadcastingStrategy(this IServiceCollection services)
    {
        return services.AddConquerorEventBroadcastingStrategy(new ParallelBroadcastingStrategy(new()));
    }

    public static IServiceCollection AddParallelConquerorEventBroadcastingStrategy(this IServiceCollection services,
                                                                                   Action<ParallelEventBroadcastingStrategyConfiguration> configure)
    {
        var configuration = new ParallelEventBroadcastingStrategyConfiguration();
        configure.Invoke(configuration);
        return services.AddConquerorEventBroadcastingStrategy(new ParallelBroadcastingStrategy(configuration));
    }
}
