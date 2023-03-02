using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class InMemoryQueryTransport : IQueryTransportClient
{
    private readonly Type handlerType;
    private readonly IServiceProvider serviceProvider;

    public InMemoryQueryTransport(IServiceProvider serviceProvider, Type handlerType)
    {
        this.serviceProvider = serviceProvider;
        this.handlerType = handlerType;
    }

    public Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        where TQuery : class
    {
        var handler = (IQueryHandler<TQuery, TResponse>)serviceProvider.GetRequiredService(handlerType);
        return handler.ExecuteQuery(query, cancellationToken);
    }
}
