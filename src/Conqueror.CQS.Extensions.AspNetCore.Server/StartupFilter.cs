using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    internal sealed class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                var applicationPartManager = builder.ApplicationServices.GetRequiredService<ApplicationPartManager>();

                var commandFeatureProvider = builder.ApplicationServices.GetService<HttpCommandControllerFeatureProvider>();
                if (commandFeatureProvider != null)
                {
                    applicationPartManager.FeatureProviders.Add(commandFeatureProvider);
                }

                var queryFeatureProvider = builder.ApplicationServices.GetService<HttpQueryControllerFeatureProvider>();
                if (queryFeatureProvider != null)
                {
                    applicationPartManager.FeatureProviders.Add(queryFeatureProvider);
                }

                next(builder);
            };
        }
    }
}
