using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalPipeline<TSignal>(
    Type? handlerType,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    SignalTransportType transportType)
    : ISignalPipeline<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    private readonly List<ISignalMiddleware<TSignal>> middlewares = [];

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
        while (true)
        {
            var index = middlewares.FindIndex(m => m is TMiddleware);

            if (index < 0)
            {
                return this;
            }

            middlewares.RemoveAt(index);
        }
    }

    public ISignalPipeline<TSignal> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : ISignalMiddleware<TSignal>
    {
        var index = middlewares.FindIndex(m => m is TMiddleware);

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used");
        }

        foreach (var middleware in middlewares.OfType<TMiddleware>())
        {
            configure(middleware);
        }

        return this;
    }

    public SignalPipelineRunner<TSignal> Build(ConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    public IEnumerator<ISignalMiddleware<TSignal>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateSignalMiddleware(SignalMiddlewareFn<TSignal> middlewareFn) : ISignalMiddleware<TSignal>
    {
        public Task Execute(SignalMiddlewareContext<TSignal> ctx) => middlewareFn(ctx);
    }
}
