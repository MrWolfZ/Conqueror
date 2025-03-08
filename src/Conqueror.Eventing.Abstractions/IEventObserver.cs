using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IEventObserver;

public interface IEventObserver<TEvent> : IEventObserver
    where TEvent : class
{
    Task Handle(TEvent evt, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(IEventPipeline<TEvent> pipeline)
    {
    }
}
