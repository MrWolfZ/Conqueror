using Conqueror.Eventing.BuiltInMiddlewares.EventTypeFiltering;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="IServiceCollection" /> to add the Conqueror CQS data annotation validation middlewares.
/// </summary>
public static class ConquerorEventingEventObserverMiddlewareEventTypeFilteringServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Conqueror eventing event type filtering middleware to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the middleware to</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddConquerorEventingEventTypeFilteringMiddleware(this IServiceCollection services)
    {
        services.AddConquerorEventObserverMiddleware<EventTypeFilteringEventObserverMiddleware>(ServiceLifetime.Singleton);

        return services;
    }
}
