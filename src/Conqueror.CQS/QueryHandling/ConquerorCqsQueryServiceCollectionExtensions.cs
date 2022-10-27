using System;
using System.Linq;
using Conqueror;
using Conqueror.CQS.QueryHandling;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    internal static class ConquerorCqsQueryServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureQueryPipeline<TQueryHandler>(this IServiceCollection services, Action<IQueryPipelineBuilder> configure)
            where TQueryHandler : IQueryHandler =>
            services.ConfigureQueryPipeline(typeof(TQueryHandler), configure);

        public static IServiceCollection ConfigureQueryPipeline(this IServiceCollection services, Type handlerType, Action<IQueryPipelineBuilder> configure)
        {
            var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is QueryHandlerPipelineConfiguration c && c.HandlerType == handlerType);

            if (existingConfiguration is not null)
            {
                services.Remove(existingConfiguration);
            }

            services.AddSingleton(new QueryHandlerPipelineConfiguration(handlerType, configure));

            return services;
        }
    }
}
