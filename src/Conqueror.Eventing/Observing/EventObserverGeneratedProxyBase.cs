using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal abstract class EventObserverGeneratedProxyBase<TEvent>(IEventObserver<TEvent> target) : IEventObserver<TEvent>
    where TEvent : class
{
    public Task Handle(TEvent evt, CancellationToken cancellationToken = default)
    {
        return target.Handle(evt, cancellationToken);
    }

    public IEventObserver<TEvent> WithPipeline(Action<IEventPipeline<TEvent>> configure)
    {
        return target.WithPipeline(configure);
    }
}
