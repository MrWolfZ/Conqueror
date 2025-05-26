using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct SignalMiddlewareContext<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    private readonly List<ISignalMiddleware<TSignal>> middlewares;
    private readonly ISignalPublisher<TSignal> sender;

    public SignalMiddlewareContext(
        List<ISignalMiddleware<TSignal>> middlewares,
        ISignalPublisher<TSignal> sender)
    {
        Debug.Assert(middlewares.Count > 0, "this should only be called if there are middlewares to execute");

        this.middlewares = middlewares;
        this.sender = sender;
    }

    public required TSignal Signal { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public required IServiceProvider ServiceProvider { get; init; }

    public required ConquerorContext ConquerorContext { get; init; }

    public required SignalTransportType TransportType { get; init; }

    private int CurrentIndex { get; init; }

    public Task Next(TSignal message, CancellationToken cancellationToken)
    {
        var nextIndex = CurrentIndex + 1;
        if (nextIndex < middlewares.Count)
        {
            var updatedContext = this with
            {
                Signal = message,
                CancellationToken = cancellationToken,
                CurrentIndex = nextIndex,
            };

            return middlewares[nextIndex].Execute(updatedContext);
        }

        return sender.Publish(message, ServiceProvider, ConquerorContext, cancellationToken);
    }
}
