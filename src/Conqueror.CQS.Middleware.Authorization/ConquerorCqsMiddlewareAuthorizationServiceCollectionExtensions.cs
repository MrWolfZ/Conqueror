using Conqueror.CQS.Middleware.Authorization;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="IServiceCollection" /> to add the Conqueror CQS authorization middlewares.
/// </summary>
public static class ConquerorCqsMiddlewareAuthorizationServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Conqueror CQS authorization middlewares to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the middlewares to</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddConquerorCQSAuthorizationMiddlewares(this IServiceCollection services)
    {
        services.AddConquerorCommandMiddleware<OperationTypeAuthorizationCommandMiddleware>(ServiceLifetime.Singleton);
        services.AddConquerorQueryMiddleware<OperationTypeAuthorizationQueryMiddleware>(ServiceLifetime.Singleton);

        services.AddConquerorCommandMiddleware<PayloadAuthorizationCommandMiddleware>(ServiceLifetime.Singleton);
        services.AddConquerorQueryMiddleware<PayloadAuthorizationQueryMiddleware>(ServiceLifetime.Singleton);

        return services;
    }
}
