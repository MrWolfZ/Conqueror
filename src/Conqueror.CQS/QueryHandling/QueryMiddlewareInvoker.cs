using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewareInvoker<TConfiguration> : IQueryMiddlewareInvoker
        where TConfiguration : QueryMiddlewareConfigurationAttribute
    {
        public async Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                               QueryMiddlewareNext<TQuery, TResponse> next,
                                                               QueryHandlerMetadata metadata,
                                                               QueryMiddlewareConfigurationAttribute middlewareConfigurationAttribute,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TQuery : class
        {
            var configurationAttribute = (TConfiguration)middlewareConfigurationAttribute;

            var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration>(query, next, configurationAttribute, cancellationToken);

            var middleware = serviceProvider.GetRequiredService<QueryMiddlewareRegistry>().GetMiddleware<TConfiguration>(serviceProvider);

            return await middleware.Execute(ctx);
        }
    }
}
