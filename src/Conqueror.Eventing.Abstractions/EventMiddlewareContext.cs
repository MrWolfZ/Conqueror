using System.Threading;
using System.Threading.Tasks;

namespace Conqueror
{
    public abstract class EventMiddlewareContext<TEvent>
        where TEvent : class
    {
        public abstract TEvent Event { get; }

        public abstract CancellationToken CancellationToken { get; }

        public abstract Task Next(TEvent evt, CancellationToken cancellationToken);
    }

    public abstract class EventObserverMiddlewareContext<TEvent> : EventMiddlewareContext<TEvent>
        where TEvent : class
    {
    }

    public abstract class EventObserverMiddlewareContext<TEvent, TConfiguration> : EventMiddlewareContext<TEvent>
        where TEvent : class
    {
        public abstract TConfiguration Configuration { get; }
    }

    public abstract class EventPublisherMiddlewareContext<TEvent> : EventMiddlewareContext<TEvent>
        where TEvent : class
    {
    }
}
