using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing
{
    internal sealed class DefaultEventObserverMiddlewareContext<TEvent, TConfiguration> : EventObserverMiddlewareContext<TEvent, TConfiguration>
        where TEvent : class
        where TConfiguration : EventObserverMiddlewareConfigurationAttribute
    {
        private readonly EventObserverMiddlewareNext<TEvent> next;

        public DefaultEventObserverMiddlewareContext(TEvent evt, EventObserverMiddlewareNext<TEvent> next, TConfiguration configuration, CancellationToken cancellationToken)
        {
            this.next = next;
            Event = evt;
            CancellationToken = cancellationToken;
            Configuration = configuration;
        }

        public override TEvent Event { get; }

        public override CancellationToken CancellationToken { get; }

        public override TConfiguration Configuration { get; }

        public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
    }
}
