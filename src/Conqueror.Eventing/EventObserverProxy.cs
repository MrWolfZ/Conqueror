using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing
{
    internal sealed class EventObserverProxy<TEvent> : IEventObserver<TEvent>
        where TEvent : class
    {
        private readonly EventMiddlewaresInvoker invoker;
        private readonly EventObserverRegistry registry;
        private readonly IServiceProvider serviceProvider;

        public EventObserverProxy(EventObserverRegistry registry, EventMiddlewaresInvoker invoker, IServiceProvider serviceProvider)
        {
            this.registry = registry;
            this.invoker = invoker;
            this.serviceProvider = serviceProvider;
        }

        public Task HandleEvent(TEvent evt, CancellationToken cancellationToken)
        {
            var metadataCol = registry.GetEventObserversMetadata<TEvent>();
            return invoker.InvokeMiddlewares(serviceProvider, metadataCol, evt, cancellationToken);
        }
    }
}
