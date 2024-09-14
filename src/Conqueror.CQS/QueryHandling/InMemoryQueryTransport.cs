using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class InMemoryQueryTransport(Type handlerType, Delegate? configurePipeline) : IQueryTransportClient
{
    public QueryTransportType TransportType => new(InMemoryQueryTransportTypeExtensions.TransportName, QueryTransportRole.Client);

    public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                           IServiceProvider serviceProvider,
                                                           CancellationToken cancellationToken)
        where TQuery : class
    {
        var proxy = new QueryHandlerProxy<TQuery, TResponse>(serviceProvider, new(new HandlerInvoker(handlerType)),
                                                             configurePipeline as Action<IQueryPipeline<TQuery, TResponse>>);

        return proxy.ExecuteQuery(query, cancellationToken);
    }

    private sealed class HandlerInvoker(Type handlerType) : IQueryTransportClient
    {
        public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            where TQuery : class
        {
            var handler = (IQueryHandler<TQuery, TResponse>)serviceProvider.GetRequiredService(handlerType);
            return handler.ExecuteQuery(query, cancellationToken);
        }

        public QueryTransportType TransportType => new(InMemoryQueryTransportTypeExtensions.TransportName, QueryTransportRole.Server);
    }
}
