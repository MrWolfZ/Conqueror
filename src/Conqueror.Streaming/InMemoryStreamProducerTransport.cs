using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class InMemoryStreamProducerTransport(Type producerType) : IStreamProducerTransportClient
{
    public IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                   IServiceProvider serviceProvider,
                                                                   CancellationToken cancellationToken)
        where TRequest : class
    {
        var producer = (IStreamProducer<TRequest, TItem>)serviceProvider.GetRequiredService(producerType);
        return producer.ExecuteRequest(request, cancellationToken);
    }
}
