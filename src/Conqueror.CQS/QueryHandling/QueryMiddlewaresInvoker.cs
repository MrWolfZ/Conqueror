using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewaresInvoker
    {
        public async Task<TResponse> InvokeMiddlewares<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                          IQueryHandler<TQuery, TResponse> handler,
                                                                          QueryHandlerMetadata metadata,
                                                                          TQuery command,
                                                                          CancellationToken cancellationToken)
            where TQuery : class
        {
            var index = 0;
            var invokers = metadata.MiddlewareConfigurationAttributes
                                   .Keys
                                   .Select(a => serviceProvider.GetService(typeof(QueryMiddlewareInvoker<>).MakeGenericType(a)))
                                   .Cast<IQueryMiddlewareInvoker>()
                                   .ToList();

            return await ExecuteNextMiddleware(command, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(TQuery cmd, CancellationToken token)
            {
                if (index >= invokers.Count)
                {
                    return await handler.ExecuteQuery(cmd, token);
                }

                var middlewareInvoker = invokers[index++];
                return await middlewareInvoker.Invoke(cmd, ExecuteNextMiddleware, metadata, serviceProvider, token);
            }
        }
    }
}
