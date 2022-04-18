using System.Threading;
using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.Eventing
{
    public interface IEventObserver
    {
    }

    public interface IEventObserver<in TEvent> : IEventObserver
        where TEvent : class
    {
        Task HandleEvent(TEvent evt, CancellationToken cancellationToken);
    }
}
