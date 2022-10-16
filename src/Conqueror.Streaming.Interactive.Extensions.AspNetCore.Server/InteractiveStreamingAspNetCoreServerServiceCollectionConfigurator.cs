using System;
using System.Linq;
using Conqueror.Common;
using Conqueror.Streaming.Interactive.Common;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Server
{
    internal sealed class InteractiveStreamingAspNetCoreServerServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public int ConfigurationPhase => 2;

        public void Configure(IServiceCollection services)
        {
            var applicationPartManager = GetServiceFromCollection<ApplicationPartManager>(services);

            if (applicationPartManager == null)
            {
                throw new InvalidOperationException("the ASP Core application part manager must be registered before configuring the Conqueror interactive streaming ASP Core Server services");
            }

            if (!applicationPartManager.FeatureProviders.Any(p => p is HttpQueryControllerFeatureProvider))
            {
                var handlerMetadata = services.Select(d => d.ImplementationInstance).OfType<InteractiveStreamingHandlerMetadata>();
                applicationPartManager.FeatureProviders.Add(new HttpQueryControllerFeatureProvider(new(), handlerMetadata));
            }
        }

        private static T? GetServiceFromCollection<T>(IServiceCollection services)
        {
            return (T?)services.LastOrDefault(d => d.ServiceType == typeof(T))?.ImplementationInstance;
        }
    }
}
