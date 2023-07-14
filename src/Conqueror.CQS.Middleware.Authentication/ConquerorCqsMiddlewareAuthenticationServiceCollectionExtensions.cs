using Conqueror.CQS.Middleware.Authentication;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Extension methods for <see cref="IServiceCollection" /> to add the Conqueror CQS authentication middlewares.
/// </summary>
public static class ConquerorCqsMiddlewareAuthenticationServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Conqueror CQS authentication middlewares to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the middlewares to</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddConquerorCQSAuthenticationMiddlewares(this IServiceCollection services)
    {
        services.AddConquerorCommonMiddlewareAuthentication();

        services.AddConquerorCommandMiddleware<AuthenticationCommandMiddleware>(ServiceLifetime.Singleton);
        services.AddConquerorQueryMiddleware<AuthenticationQueryMiddleware>(ServiceLifetime.Singleton);

        return services;
    }
}
