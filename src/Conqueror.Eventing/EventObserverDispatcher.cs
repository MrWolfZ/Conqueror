using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventObserverDispatcher<TEvent> : IEventObserver<TEvent>
    where TEvent : class
{
    private readonly IConquerorEventDispatcher dispatcher;

    public EventObserverDispatcher(IConquerorEventDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default)
    {
        return dispatcher.DispatchEvent(evt, cancellationToken);
    }
}
