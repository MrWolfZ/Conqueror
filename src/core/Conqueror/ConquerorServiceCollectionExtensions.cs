#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using Conqueror;
using Conqueror.Context;

public static class ConquerorServiceCollectionExtensions
{
    public static IServiceCollection AddConqueror(this IServiceCollection services) =>
        services.AddConquerorMessaging().AddConquerorSignalling().AddConquerorIterating();

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
}
