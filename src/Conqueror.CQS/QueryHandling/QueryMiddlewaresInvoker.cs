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
            var attributes = metadata.MiddlewareConfigurationAttributes.ToList();

            return await ExecuteNextMiddleware(0, command, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(int index, TQuery query, CancellationToken token)
            {
                if (index >= attributes.Count)
                {
                    return await handler.ExecuteQuery(query, token);
                }

                var attribute = attributes[index];
                var invoker = (IQueryMiddlewareInvoker)serviceProvider.GetService(typeof(QueryMiddlewareInvoker<>).MakeGenericType(attribute.GetType()))!;

                return await invoker.Invoke(query, (q, t) => ExecuteNextMiddleware(index + 1, q, t), metadata, attribute, serviceProvider, token);
            }
        }
    }
}
