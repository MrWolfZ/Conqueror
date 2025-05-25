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
        TSignal initialSignal,
        ISignalPublisher<TSignal> publisher,
        SignalTransportType transportType,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return publisher.Publish(
                initialSignal,
                serviceProvider,
                conquerorContext,
                cancellationToken);
        }

        Task Publish(TSignal message, CancellationToken token) => publisher.Publish(
            message,
            serviceProvider,
            conquerorContext,
            token);

        SignalMiddlewareNext<TSignal> next = Publish;

        for (var i = middlewares.Count - 1; i >= 0; i -= 1)
        {
            var middleware = middlewares[i];
            var nextToCall = next;
            next = Next;

            Task Next(TSignal message, CancellationToken token)
                => middleware.Execute(
                    new DefaultSignalMiddlewareContext<TSignal>(
                        message,
                        nextToCall,
                        serviceProvider,
                        conquerorContext,
                        transportType,
                        token));
        }

        return next(initialSignal, cancellationToken);
    }
}
