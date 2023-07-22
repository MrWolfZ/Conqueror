using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class ConquerorEventMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract TEvent Event { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract IConquerorContext ConquerorContext { get; }

    public abstract Task Next(TEvent evt, CancellationToken cancellationToken);
}

public abstract class EventObserverMiddlewareContext<TEvent> : ConquerorEventMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract Type ObservedEventType { get; }
}

public abstract class EventObserverMiddlewareContext<TEvent, TConfiguration> : EventObserverMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract TConfiguration Configuration { get; }
}

public abstract class EventPublisherMiddlewareContext<TEvent> : ConquerorEventMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract Type PublisherType { get; }

    public abstract string PublisherName { get; }
}

public abstract class EventPublisherMiddlewareContext<TEvent, TConfiguration> : EventPublisherMiddlewareContext<TEvent>
    where TEvent : class
{
    public abstract TConfiguration Configuration { get; }
}
