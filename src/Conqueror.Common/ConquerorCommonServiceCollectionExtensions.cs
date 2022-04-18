using System.Linq;
using Conqueror.Util;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCommonServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureConqueror(this IServiceCollection services)
        {
            var configurators = services.Select(d => d.ImplementationInstance)
                                        .OfType<IServiceCollectionConfigurator>()
                                        .ToList();

            foreach (var configurator in configurators)
            {
                configurator.Configure(services);
            }

            return services;
        }
    }
}
