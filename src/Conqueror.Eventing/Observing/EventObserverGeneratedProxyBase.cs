using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal abstract class EventObserverGeneratedProxyBase<TEvent>(IEventObserver<TEvent> target) : IEventObserver<TEvent>
    where TEvent : class
{
    public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default)
    {
        return target.HandleEvent(evt, cancellationToken);
    }
}
