using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class InMemoryQueryTransport : IQueryTransportClient
{
    private readonly Type handlerType;

    public InMemoryQueryTransport(Type handlerType)
    {
        this.handlerType = handlerType;
    }

    public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                           IServiceProvider serviceProvider,
                                                           CancellationToken cancellationToken)
        where TQuery : class
    {
        var handler = (IQueryHandler<TQuery, TResponse>)serviceProvider.GetRequiredService(handlerType);
        return handler.ExecuteQuery(query, cancellationToken);
    }
}
