using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPipeline<TEventNotification>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    EventNotificationTransportType transportType)
    : IEventNotificationPipeline<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    private readonly List<IEventNotificationMiddleware<TEventNotification>> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public EventNotificationTransportType TransportType { get; } = transportType;

    public int Count => middlewares.Count;

    public IEventNotificationPipeline<TEventNotification> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IEventNotificationMiddleware<TEventNotification>
    {
        middlewares.Add(middleware);
        return this;
    }

    public IEventNotificationPipeline<TEventNotification> Use(EventNotificationMiddlewareFn<TEventNotification> middlewareFn)
    {
        return Use(new DelegateEventNotificationMiddleware(middlewareFn));
    }

    public IEventNotificationPipeline<TEventNotification> Without<TMiddleware>()
        where TMiddleware : IEventNotificationMiddleware<TEventNotification>
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

    public IEventNotificationPipeline<TEventNotification> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IEventNotificationMiddleware<TEventNotification>
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

    public EventNotificationPipelineRunner<TEventNotification> Build(ConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    public IEnumerator<IEventNotificationMiddleware<TEventNotification>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class DelegateEventNotificationMiddleware(EventNotificationMiddlewareFn<TEventNotification> middlewareFn) : IEventNotificationMiddleware<TEventNotification>
    {
        public Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx) => middlewareFn(ctx);
    }
}
