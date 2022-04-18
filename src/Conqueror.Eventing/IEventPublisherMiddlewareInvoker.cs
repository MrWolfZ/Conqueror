using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing
{
    internal delegate Task EventPublisherMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

    internal interface IEventPublisherMiddlewareInvoker
    {
        Task Invoke<TEvent>(TEvent evt,
                            EventPublisherMiddlewareNext<TEvent> next,
                            IServiceProvider serviceProvider,
                            CancellationToken cancellationToken)
            where TEvent : class;
    }
}
