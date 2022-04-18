using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing
{
    internal sealed class DefaultEventPublisherMiddlewareContext<TEvent> : EventPublisherMiddlewareContext<TEvent>
        where TEvent : class
    {
        private readonly EventPublisherMiddlewareNext<TEvent> next;

        public DefaultEventPublisherMiddlewareContext(TEvent evt, EventPublisherMiddlewareNext<TEvent> next, CancellationToken cancellationToken)
        {
            this.next = next;
            Event = evt;
            CancellationToken = cancellationToken;
        }

        public override TEvent Event { get; }

        public override CancellationToken CancellationToken { get; }

        public override Task Next(TEvent evt, CancellationToken cancellationToken) => next(evt, cancellationToken);
    }
}
