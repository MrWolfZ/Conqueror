using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalPipelineRunner<TSignal>(
    ConquerorContext conquerorContext,
    List<ISignalMiddleware<TSignal>> middlewares)
    where TSignal : class, ISignal<TSignal>
{
    private readonly List<ISignalMiddleware<TSignal>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public async Task Execute(IServiceProvider serviceProvider,
                              TSignal initialSignal,
                              ISignalPublisher<TSignal> publisher,
                              SignalTransportType transportType,
                              CancellationToken cancellationToken)
    {
        var next = (TSignal signal, CancellationToken token) => publisher.Publish(signal, serviceProvider, conquerorContext, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (signal, token) =>
            {
                var ctx = new DefaultSignalMiddlewareContext<TSignal>(signal,
                                                                      (c, t) => nextToCall(c, t),
                                                                      serviceProvider,
                                                                      conquerorContext,
                                                                      transportType,
                                                                      token);

                return middleware.Execute(ctx);
            };
        }

        await next(initialSignal, cancellationToken).ConfigureAwait(false);
    }
}
