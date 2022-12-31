using System;
using System.Linq;
using Conqueror;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCommonServiceCollectionExtensions
    {
        public static IServiceCollection FinalizeConquerorRegistrations(this IServiceCollection services)
        {
            if (services.Any(d => d.ServiceType == typeof(WasFinalized)))
            {
                throw new InvalidOperationException($"'{nameof(FinalizeConquerorRegistrations)}' was called multiple times, but it must only be called once");
            }

            _ = services.AddSingleton<WasFinalized>();

            var configurators = services.Select(d => d.ImplementationInstance)
                                        .OfType<IServiceCollectionConfigurator>()
                                        .OrderBy(c => c.ConfigurationPhase)
                                        .ToList();

            var finalizationCheck = services.SingleOrDefault(d => d.ServiceType == typeof(DidYouForgetToCallFinalizeConquerorRegistrations));

            if (finalizationCheck != null)
            {
                _ = services.Remove(finalizationCheck);
            }

            foreach (var configurator in configurators)
            {
                configurator.Configure(services);
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

        private sealed class WasFinalized
        {
        }
    }
}
