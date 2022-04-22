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
            var attributes = metadata.MiddlewareConfigurationAttributes.ToList();

            return await ExecuteNextMiddleware(command, cancellationToken);

            async Task<TResponse> ExecuteNextMiddleware(TQuery cmd, CancellationToken token)
            {
                if (index >= attributes.Count)
                {
                    return await handler.ExecuteQuery(cmd, token);
                }

                var attribute = attributes[index++];
                var invoker = (IQueryMiddlewareInvoker)serviceProvider.GetService(typeof(QueryMiddlewareInvoker<>).MakeGenericType(attribute.GetType()))!;

                return await invoker.Invoke(cmd, ExecuteNextMiddleware, metadata, attribute, serviceProvider, token);
            }
        }
    }
}
