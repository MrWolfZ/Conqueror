using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamConsumerPipeline(
    ConquerorContext conquerorContext,
    List<(Type MiddlewareType, object? MiddlewareConfiguration, IStreamConsumerMiddlewareInvoker Invoker)> middlewares)
{
    public Task Execute<TItem>(IServiceProvider serviceProvider,
                               TItem initialItem,
                               Type? consumerType,
                               object? key,
                               IStreamConsumer<TItem>? consumerInstance,
                               CancellationToken cancellationToken)
    {
        return ExecuteNextMiddleware(0, initialItem, conquerorContext, cancellationToken);

        async Task ExecuteNextMiddleware(int index, TItem item, ConquerorContext ctx, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                var consumer = CreateConsumer();
                await consumer.HandleItem(item, token).ConfigureAwait(false);
                return;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            await invoker.Invoke(item, (c, t) => ExecuteNextMiddleware(index + 1, c, ctx, t), middlewareConfiguration, serviceProvider, ctx, token).ConfigureAwait(false);
        }

        IStreamConsumer<TItem> CreateConsumer()
        {
            if (consumerInstance is not null)
            {
                return consumerInstance;
            }

            if (key is null)
            {
                return (IStreamConsumer<TItem>)serviceProvider.GetRequiredService(consumerType!);
            }

            return (IStreamConsumer<TItem>)serviceProvider.GetRequiredKeyedService(consumerType!, key);
        }
    }
}
