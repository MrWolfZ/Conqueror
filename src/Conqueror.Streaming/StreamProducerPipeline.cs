using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class StreamProducerPipeline(
    IConquerorContext conquerorContext,
    List<(Type MiddlewareType, object? MiddlewareConfiguration, IStreamProducerMiddlewareInvoker Invoker)> middlewares)
{
    public IAsyncEnumerable<TItem> Execute<TRequest, TItem>(IServiceProvider serviceProvider,
                                                            TRequest initialRequest,
                                                            StreamProducerTransportClientFactory transportClientFactory,
                                                            CancellationToken cancellationToken)
        where TRequest : class
    {
        return ExecuteNextMiddleware(0, initialRequest, conquerorContext, cancellationToken);

        async IAsyncEnumerable<TItem> ExecuteNextMiddleware(int index, TRequest request, IConquerorContext ctx, [EnumeratorCancellation] CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                var transportClient = await transportClientFactory.Create(typeof(TRequest), serviceProvider).ConfigureAwait(false);
                await foreach (var item in transportClient.ExecuteRequest<TRequest, TItem>(request, serviceProvider, token).ConfigureAwait(false))
                {
                    yield return item;
                }

                yield break;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];

            await foreach (var item in invoker.Invoke(request, (q, t) => ExecuteNextMiddleware(index + 1, q, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }
}
