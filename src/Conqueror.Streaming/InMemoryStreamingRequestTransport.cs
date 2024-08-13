using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class InMemoryStreamingRequestTransport : IStreamingRequestTransportClient
{
    private readonly Type handlerType;

    public InMemoryStreamingRequestTransport(Type handlerType)
    {
        this.handlerType = handlerType;
    }

    public IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                   IServiceProvider serviceProvider,
                                                                   CancellationToken cancellationToken)
        where TRequest : class
    {
        var handler = (IStreamingRequestHandler<TRequest, TItem>)serviceProvider.GetRequiredService(handlerType);
        return handler.ExecuteRequest(request, cancellationToken);
    }
}
