using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

internal sealed class EventObserverDispatcher<TEvent>(IConquerorEventDispatcher dispatcher) : IEventObserver<TEvent>
    where TEvent : class
{
    public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default)
    {
        return dispatcher.DispatchEvent(evt, cancellationToken);
    }
}
