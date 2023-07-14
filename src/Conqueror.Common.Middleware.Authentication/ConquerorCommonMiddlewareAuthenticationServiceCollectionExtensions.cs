using Conqueror;
using Conqueror.Common.Middleware.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCommonMiddlewareAuthenticationServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the common services for all authentication middlewares. This method should typically NOT be
    ///     called directly by user code, since it is called already when adding the authentication middleware
    ///     services.
    /// </summary>
    public static IServiceCollection AddConquerorCommonMiddlewareAuthentication(this IServiceCollection services)
    {
        services.TryAddSingleton<IConquerorAuthenticationContext, ConquerorAuthenticationContext>();

        return services;
    }
}
