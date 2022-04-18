using System.Linq;
using Conqueror.CQS.Extensions.AspNetCore.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcBuilderExtensions
    {
        public static IMvcBuilder AddConqueror(this IMvcBuilder builder)
        {
            builder.Services.AddHttpCommandServices();
            builder.Services.AddHttpQueryServices();

            return builder;
        }

        private static void AddHttpCommandServices(this IServiceCollection services)
        {
            services.TryAddStartupFilter();
            services.TryAddSingleton<HttpCommandControllerFeatureProvider>();
            services.TryAddSingleton<DynamicCommandControllerFactory>();
        }

        private static void AddHttpQueryServices(this IServiceCollection services)
        {
            services.TryAddStartupFilter();
            services.TryAddSingleton<HttpQueryControllerFeatureProvider>();
            services.TryAddSingleton<DynamicQueryControllerFactory>();
        }

        private static void TryAddStartupFilter(this IServiceCollection services)
        {
            if (services.Where(d => d.ServiceType == typeof(IStartupFilter)).Any(d => d.ImplementationType == typeof(StartupFilter)))
            {
                return;
            }

            _ = services.AddSingleton<IStartupFilter, StartupFilter>();
        }
    }
}
