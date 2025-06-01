using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalPipeline<TSignal>(
    Type? handlerType,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    SignalTransportType transportType,
    int initialCapacity)
    : ISignalPipeline<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    private readonly List<ISignalMiddleware<TSignal>> middlewares = new(initialCapacity);

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public SignalTransportType TransportType { get; } = transportType;

    public int Count => middlewares.Count;

    public ISignalPipeline<TSignal> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        middlewares.Add(middleware);

        return this;
    }

    public ISignalPipeline<TSignal> Use(SignalMiddlewareFn<TSignal> middlewareFn)
    {
        return Use(new DelegateSignalMiddleware(middlewareFn));
    }

    public ISignalPipeline<TSignal> Without<TMiddleware>()
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        _ = middlewares.RemoveAll(static m => m is TMiddleware);

        return this;
    }

    public ISignalPipeline<TSignal> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        var found = false;
        foreach (var middleware in middlewares)
        {
            if (middleware is TMiddleware m)
            {
                configure(m);
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException($"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used");
        }

        return this;
    }

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
                ConquerorContext,
                cancellationToken);
        }

        var ctx = new SignalMiddlewareContext<TSignal>(middlewares, publisher)
        {
            Signal = signal,
            TransportType = transportType,
            CancellationToken = cancellationToken,
            ConquerorContext = ConquerorContext,
            ServiceProvider = serviceProvider,
        };

        return middlewares[0].Execute(ctx);
    }

    public IEnumerator<ISignalMiddleware<TSignal>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateSignalMiddleware(SignalMiddlewareFn<TSignal> middlewareFn) : ISignalMiddleware<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx) => middlewareFn(ctx);
    }
}
