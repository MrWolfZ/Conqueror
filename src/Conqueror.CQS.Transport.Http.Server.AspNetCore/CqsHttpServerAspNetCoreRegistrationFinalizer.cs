using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class CqsHttpServerAspNetCoreRegistrationFinalizer : IConquerorRegistrationFinalizer
    {
        private readonly IServiceCollection services;

        public CqsHttpServerAspNetCoreRegistrationFinalizer(IServiceCollection services)
        {
            this.services = services;
        }

        public int ExecutionPhase => 2;

        public void Execute()
        {
            var applicationPartManager = GetServiceFromCollection<ApplicationPartManager>(services);

            if (applicationPartManager is null)
            {
                throw new InvalidOperationException("the ASP Core application part manager must be registered before finalizing the Conqueror CQS ASP Core Server services");
            }

            if (!applicationPartManager.FeatureProviders.Any(p => p is HttpEndpointControllerFeatureProvider))
            {
                var commandHandlerRegistry = GetServiceFromCollection<ICommandHandlerRegistry>(services);
                var queryHandlerRegistry = GetServiceFromCollection<IQueryHandlerRegistry>(services);
                var options = GetServiceFromCollection<ConquerorCqsHttpTransportServerAspNetCoreOptions>(services);

                if (commandHandlerRegistry is null || queryHandlerRegistry is null)
                {
                    throw new InvalidOperationException("the Conqueror CQS services must be registered before finalizing the Conqueror CQS ASP Core Server services");
                }

                if (options is null)
                {
                    throw new InvalidOperationException("the Conqueror CQS ASP Core Server services must be registered before finalizing the Conqueror CQS ASP Core Server services");
                }

                var endpointRegistry = new HttpEndpointRegistry(commandHandlerRegistry, queryHandlerRegistry, options);
                applicationPartManager.FeatureProviders.Add(new HttpEndpointControllerFeatureProvider(endpointRegistry.GetEndpoints()));
            }
        }

        private static T? GetServiceFromCollection<T>(IServiceCollection services)
        {
            return (T?)services.LastOrDefault(d => d.ImplementationInstance?.GetType().IsAssignableTo(typeof(T)) ?? false)?.ImplementationInstance;
        }
    }
}
