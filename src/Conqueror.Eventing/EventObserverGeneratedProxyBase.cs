using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal abstract class EventObserverGeneratedProxyBase<TEvent> : IEventObserver<TEvent>
    where TEvent : class
{
    private readonly IEventObserver<TEvent> target;

    protected EventObserverGeneratedProxyBase(IEventObserver<TEvent> target)
    {
        this.target = target;
    }

    public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default)
    {
        return target.HandleEvent(evt, cancellationToken);
    }
}
