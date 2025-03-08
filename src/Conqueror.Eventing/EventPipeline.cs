using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventPipeline<TEvent, TObservedEvent>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    EventTransportType transportType)
    : IEventPipeline<TObservedEvent>
    where TEvent : class, TObservedEvent
    where TObservedEvent : class
{
    private readonly List<IEventMiddleware<TObservedEvent>> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public EventTransportType TransportType { get; } = transportType;

    public int Count => middlewares.Count;

    public IEventPipeline<TObservedEvent> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IEventMiddleware<TObservedEvent>
    {
        middlewares.Add(middleware);
        return this;
    }

    public IEventPipeline<TObservedEvent> Use(EventMiddlewareFn<TObservedEvent> middlewareFn)
    {
        return Use(new DelegateEventMiddleware(middlewareFn));
    }

    public IEventPipeline<TObservedEvent> Without<TMiddleware>()
        where TMiddleware : IEventMiddleware<TObservedEvent>
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

    public IEventPipeline<TObservedEvent> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IEventMiddleware<TObservedEvent>
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

    public EventPipelineRunner<TEvent, TObservedEvent> Build(ConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    public IEnumerator<IEventMiddleware<TObservedEvent>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateEventMiddleware(EventMiddlewareFn<TObservedEvent> middlewareFn) : IEventMiddleware<TObservedEvent>
    {
        public Task Execute(EventMiddlewareContext<TObservedEvent> ctx) => middlewareFn(ctx);
    }
}
