using System;
using Conqueror;
using Conqueror.Eventing.Observing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingTransportReceiverServiceCollectionExtensions
{
    // TODO: should this add it enumerable and allow configuring it multiple times?
    public static IServiceCollection ConfigureInProcessEventBroadcastingStrategy(this IServiceCollection services,
                                                                                 Action<IConquerorEventBroadcastingStrategyBuilder> configureStrategy)
    {
        services.Replace(ServiceDescriptor.Singleton(new InProcessEventTransportReceiver.Configuration(configureStrategy)));
        return services;
    }

    internal static void AddInProcessEventTransportReceiver(this IServiceCollection services)
    {
        services.TryAddSingleton<InProcessEventTransportReceiver>();
    }
}
