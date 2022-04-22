using System.Threading;
using System.Threading.Tasks;

// these classes belong together
#pragma warning disable SA1402

namespace Conqueror.Eventing
{
    public abstract class EventMiddlewareContext<TEvent>
        where TEvent : class
    {
        public abstract TEvent Event { get; }

        public abstract CancellationToken CancellationToken { get; }

        public abstract Task Next(TEvent evt, CancellationToken cancellationToken);
    }

    public abstract class EventObserverMiddlewareContext<TEvent, TConfiguration> : EventMiddlewareContext<TEvent>
        where TEvent : class
        where TConfiguration : EventObserverMiddlewareConfigurationAttribute
    {
        public abstract TConfiguration Configuration { get; }
    }

    public abstract class EventPublisherMiddlewareContext<TEvent> : EventMiddlewareContext<TEvent>
        where TEvent : class
    {
    }
}
