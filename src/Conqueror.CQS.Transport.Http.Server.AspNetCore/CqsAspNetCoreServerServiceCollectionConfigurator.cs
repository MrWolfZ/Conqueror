using System;
using System.Linq;
using Conqueror.Common;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class CqsAspNetCoreServerServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public int ConfigurationPhase => 2;

        public void Configure(IServiceCollection services)
        {
            var applicationPartManager = GetServiceFromCollection<ApplicationPartManager>(services);

            if (applicationPartManager == null)
            {
                throw new InvalidOperationException("the ASP Core application part manager must be registered before configuring the Conqueror CQS ASP Core Server services");
            }

            if (!applicationPartManager.FeatureProviders.Any(p => p is HttpCommandControllerFeatureProvider))
            {
                var commandHandlerRegistry = services.Select(d => d.ImplementationInstance).OfType<ICommandHandlerRegistry>().Single();
                applicationPartManager.FeatureProviders.Add(new HttpCommandControllerFeatureProvider(new(), commandHandlerRegistry));
            }

            if (!applicationPartManager.FeatureProviders.Any(p => p is HttpQueryControllerFeatureProvider))
            {
                var queryHandlerRegistry = services.Select(d => d.ImplementationInstance).OfType<IQueryHandlerRegistry>().Single();
                applicationPartManager.FeatureProviders.Add(new HttpQueryControllerFeatureProvider(new(), queryHandlerRegistry));
            }
        }

        private static T? GetServiceFromCollection<T>(IServiceCollection services)
        {
            return (T?)services.LastOrDefault(d => d.ServiceType == typeof(T))?.ImplementationInstance;
        }
    }
}
