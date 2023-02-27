using Conqueror.CQS.Middleware.Polly;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extension methods for <see cref="IServiceCollection" /> to add the Conqueror CQS Polly middlewares.
    /// </summary>
    public static class ConquerorCqsMiddlewarePollyServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds the Conqueror CQS Polly middlewares to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the middlewares to</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddConquerorCQSPollyMiddlewares(this IServiceCollection services)
        {
            services.AddConquerorCommandMiddleware<PollyCommandMiddleware>(ServiceLifetime.Singleton);
            services.AddConquerorQueryMiddleware<PollyQueryMiddleware>(ServiceLifetime.Singleton);

            return services;
        }
    }
}
