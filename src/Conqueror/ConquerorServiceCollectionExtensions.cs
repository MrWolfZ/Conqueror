// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorServiceCollectionExtensions
{
    public static IServiceCollection AddConqueror(this IServiceCollection services)
    {
        services.AddConquerorMessaging();
        services.AddConquerorContext();

        return services;
    }
}
