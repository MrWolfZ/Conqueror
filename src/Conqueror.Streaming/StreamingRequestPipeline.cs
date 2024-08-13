using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestPipeline
{
    private readonly IConquerorContext conquerorContext;

    public StreamingRequestPipeline(IConquerorContext conquerorContext)
    {
        this.conquerorContext = conquerorContext;
    }

    public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(IServiceProvider serviceProvider,
                                                                  TRequest initialRequest,
                                                                  StreamingRequestTransportClientFactory transportClientFactory,
                                                                  [EnumeratorCancellation] CancellationToken cancellationToken)
        where TRequest : class
    {
        _ = conquerorContext;
        var transportClient = await transportClientFactory.Create(typeof(TRequest), serviceProvider).ConfigureAwait(false);

        await foreach (var item in transportClient.ExecuteRequest<TRequest, TItem>(initialRequest, serviceProvider, cancellationToken))
        {
            yield return item;
        }

        // return ExecuteNextMiddleware(0, initialStreamingRequest, conquerorContext, cancellationToken);
        //
        // async IAsyncEnumerable<TItem> ExecuteNextMiddleware(int index, TRequest request, IConquerorContext ctx, CancellationToken token)
        // {
        //     if (index >= middlewares.Count)
        //     {
        //         var transportClient = await transportClientFactory.Create(typeof(TRequest), serviceProvider).ConfigureAwait(false);
        //         return transportClient.ExecuteRequest<TRequest, TItem>(request, serviceProvider, token).ConfigureAwait(false);
        //     }
        //
        //     var (_, middlewareConfiguration, invoker) = middlewares[index];
        //     return await invoker.Invoke(request, (q, t) => ExecuteNextMiddleware(index + 1, q, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
        // }
    }
}
