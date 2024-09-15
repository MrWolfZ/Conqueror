using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IEventObserver;

public interface IEventObserver<in TEvent> : IEventObserver
    where TEvent : class
{
    Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default);
}

/// <summary>
///     Note that this interface cannot be merged into <see cref="IEventObserver" /> since it would
///     disallow that interface to be used as generic parameter (see also this GitHub issue:
///     https://github.com/dotnet/csharplang/issues/5955).
/// </summary>
public interface IConfigureEventObserverPipeline
{
    static abstract void ConfigurePipeline(IEventObserverPipelineBuilder pipeline);
}
