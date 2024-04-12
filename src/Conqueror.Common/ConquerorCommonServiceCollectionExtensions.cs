using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Conqueror;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCommonServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the services required for interacting with the Conqueror context. This method does typically not need to be
    ///     called from user code, since it is called from other Conqueror registration logic.
    /// </summary>
    /// <param name="services">The service collection to add the Conqueror context services to</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddConquerorContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IConquerorContextAccessor, DefaultConquerorContextAccessor>();

        return services;
    }

    public static IServiceCollection FinalizeConquerorRegistrations(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(WasFinalized)))
        {
            throw new InvalidOperationException($"'{nameof(FinalizeConquerorRegistrations)}' was called multiple times, but it must only be called once");
        }

        _ = services.AddSingleton<WasFinalized>();

        var configurators = services.Select(d => d.ImplementationInstance)
                                    .OfType<IConquerorRegistrationFinalizer>()
                                    .OrderBy(c => c.ExecutionPhase)
                                    .ToList();

        var finalizationCheck = services.SingleOrDefault(d => d.ServiceType == typeof(DidYouForgetToCallFinalizeConquerorRegistrations));

        if (finalizationCheck != null)
        {
            _ = services.Remove(finalizationCheck);
        }

        foreach (var configurator in configurators)
        {
            configurator.Execute();
        }

        return services;
    }

    internal static IServiceCollection AddFinalizationCheck(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(WasFinalized)))
        {
            throw new InvalidOperationException($"no more Conqueror services can be added after '{nameof(FinalizeConquerorRegistrations)}' was called");
        }

        services.TryAddSingleton<DidYouForgetToCallFinalizeConquerorRegistrations>();

        return services;
    }

    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "This is a marker type that is fine to be empty.")]
    private sealed class WasFinalized
    {
    }
}
