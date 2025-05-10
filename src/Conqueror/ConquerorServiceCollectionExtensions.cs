using Conqueror;
using Conqueror.Context;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorServiceCollectionExtensions
{
    public static IServiceCollection AddConqueror(this IServiceCollection services)
    {
        return services.AddConquerorMessaging()
                       .AddConquerorSignalling();
    }

    /// <summary>
    ///     Adds the services required for interacting with the Conqueror context. This method does typically not need to be
    ///     called from user code, since it is called from other Conqueror registration logic.
    /// </summary>
    /// <param name="services">The service collection to add the Conqueror context services to</param>
    /// <returns>The service collection</returns>
    // TODO: make internal once streaming is converted to iterators
    public static IServiceCollection AddConquerorContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IConquerorContextAccessor, DefaultConquerorContextAccessor>();

        return services;
    }

    internal static IServiceCollection AddConquerorSingletons(this IServiceCollection services)
    {
        services.TryAddSingleton<ConquerorSingletons>();

        return services;
    }
}
