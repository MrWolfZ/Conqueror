using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal interface IStreamProducerMiddlewareInvoker
{
    Type MiddlewareType { get; }

    IAsyncEnumerable<TItem> Invoke<TRequest, TItem>(TRequest request,
                                                    StreamProducerMiddlewareNext<TRequest, TItem> next,
                                                    object? middlewareConfiguration,
                                                    IServiceProvider serviceProvider,
                                                    IConquerorContext conquerorContext,
                                                    CancellationToken cancellationToken)
        where TRequest : class;
}
