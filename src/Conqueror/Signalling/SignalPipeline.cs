using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalPipeline<TSignal>(
    Type? handlerType,
    IServiceProvider serviceProvider,
    SignalTransportType transportType,
    int initialCapacity)
    : ISignalPipeline<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    private readonly List<ISignalMiddleware<TSignal>> middlewares = new(initialCapacity);

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public SignalTransportType TransportType { get; } = transportType;

    public int Count => middlewares.Count;

    internal int Capacity => middlewares.Capacity;

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

    public IConditionalSignalPipeline<TSignal> UseWhen(Predicate<SignalMiddlewareContext<TSignal>> predicate)
    {
        return new ConditionalPipeline(predicate, this);
    }

    public ISignalPipeline<TSignal> Without<TMiddleware>()
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        _ = middlewares.RemoveAll(static m => m is TMiddleware or ConditionalSignalMiddleware<TMiddleware>);

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

            if (middleware is ConditionalSignalMiddleware<TMiddleware> conditionalMiddleware)
            {
                configure(conditionalMiddleware.Middleware);
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
        TSignal signal,
        ISignalPublisher<TSignal> publisher,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return publisher.Publish(
                signal,
                ServiceProvider,
                conquerorContext,
                cancellationToken);
        }

        var ctx = new SignalMiddlewareContext<TSignal>(middlewares, publisher)
        {
            Signal = signal,
            TransportType = TransportType,
            CancellationToken = cancellationToken,
            ConquerorContext = conquerorContext,
            ServiceProvider = ServiceProvider,
        };

        return middlewares[0].Execute(ctx);
    }

    public IEnumerator<ISignalMiddleware<TSignal>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateSignalMiddleware(SignalMiddlewareFn<TSignal> middlewareFn) : ISignalMiddleware<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx) => middlewareFn(ctx);
    }

    private sealed class ConditionalSignalMiddleware<TMiddleware>(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        TMiddleware middleware)
        : ISignalMiddleware<TSignal>
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        public TMiddleware Middleware => middleware;

        public Task Execute(SignalMiddlewareContext<TSignal> ctx)
            => predicate(ctx) ? middleware.Execute(ctx) : ctx.Next(ctx.Signal, ctx.CancellationToken);
    }

    private sealed class ConditionalDelegateSignalMiddleware(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        SignalMiddlewareFn<TSignal> middlewareFn)
        : ISignalMiddleware<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx)
            => predicate(ctx) ? middlewareFn(ctx) : ctx.Next(ctx.Signal, ctx.CancellationToken);
    }

    private sealed class ConditionalPipeline(
        Predicate<SignalMiddlewareContext<TSignal>> predicate,
        SignalPipeline<TSignal> pipeline)
        : IConditionalSignalPipeline<TSignal>
    {
        public IConditionalSignalPipeline<TSignal> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : ISignalMiddleware<TSignal>
        {
            _ = pipeline.Use(new ConditionalSignalMiddleware<TMiddleware>(predicate, middleware));

            return this;
        }

        public IConditionalSignalPipeline<TSignal> Use(SignalMiddlewareFn<TSignal> middlewareFn)
        {
            _ = pipeline.Use(new ConditionalDelegateSignalMiddleware(predicate, middlewareFn));

            return this;
        }
    }
}
