using Conqueror.CQS.Middleware.DataAnnotationValidation;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Extension methods for <see cref="IServiceCollection" /> to add the Conqueror CQS data annotation validation middlewares.
    /// </summary>
    public static class ConquerorCqsMiddlewareDataAnnotationValidationServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds the Conqueror CQS data annotation validation middlewares to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add the middlewares to</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddConquerorCQSDataAnnotationValidationMiddlewares(this IServiceCollection services)
        {
            services.AddConquerorCommandMiddleware<DataAnnotationValidationCommandMiddleware>(ServiceLifetime.Singleton);
            services.AddConquerorQueryMiddleware<DataAnnotationValidationQueryMiddleware>(ServiceLifetime.Singleton);

            return services;
        }
    }
}
