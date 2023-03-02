using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal delegate Task EventObserverMiddlewareNext<in TEvent>(TEvent evt, CancellationToken cancellationToken);

internal interface IEventObserverMiddlewareInvoker
{
    Task Invoke<TEvent>(TEvent evt,
                        EventObserverMiddlewareNext<TEvent> next,
                        Type middlewareType,
                        object? middlewareConfiguration,
                        IServiceProvider serviceProvider,
                        CancellationToken cancellationToken)
        where TEvent : class;
}
