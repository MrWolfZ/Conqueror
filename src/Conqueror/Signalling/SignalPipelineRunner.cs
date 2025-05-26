using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalPipelineRunner<TSignal>(
    ConquerorContext conquerorContext,
    List<ISignalMiddleware<TSignal>> middlewares)
    where TSignal : class, ISignal<TSignal>
{
    public Task Execute(
        IServiceProvider serviceProvider,
        TSignal signal,
        ISignalPublisher<TSignal> publisher,
        SignalTransportType transportType,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return publisher.Publish(
                signal,
                serviceProvider,
                conquerorContext,
                cancellationToken);
        }

        var ctx = new SignalMiddlewareContext<TSignal>(middlewares, publisher)
        {
            Signal = signal,
            TransportType = transportType,
            CancellationToken = cancellationToken,
            ConquerorContext = conquerorContext,
            ServiceProvider = serviceProvider,
        };

        return middlewares[0].Execute(ctx);
    }
}
