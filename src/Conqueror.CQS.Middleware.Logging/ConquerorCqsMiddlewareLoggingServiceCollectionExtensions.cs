using Conqueror.CQS.Middleware.Logging;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extension methods for <see cref="IServiceCollection" /> to add the Conqueror CQS logging middlewares.
    /// </summary>
    public static class ConquerorCqsMiddlewareLoggingServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds the Conqueror CQS logging middlewares to the service collection.<br />
        ///     The middlewares requires an <see cref="Microsoft.Extensions.Logging.ILoggerFactory" />
        ///     to be available in the services.
        /// </summary>
        /// <param name="services">The service collection to add the middlewares to</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddConquerorCQSLoggingMiddlewares(this IServiceCollection services)
        {
            services.AddConquerorCommandMiddleware<CommandLoggingMiddleware>(ServiceLifetime.Singleton);
            services.AddConquerorQueryMiddleware<QueryLoggingMiddleware>(ServiceLifetime.Singleton);

            return services;
        }
    }
}
