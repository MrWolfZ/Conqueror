using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IEventObserver;

public interface IEventObserver<in TEvent> : IEventObserver
    where TEvent : class
{
    Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default);

    static virtual void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
    {
    }
}
