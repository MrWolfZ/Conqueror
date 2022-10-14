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
    
    /// <summary>
    /// Note that this interface cannot be merged into <see cref="IEventObserver"/> since it would
    /// disallow that interface to be used as generic parameter (see also this GitHub issue:
    /// https://github.com/dotnet/csharplang/issues/5955).
    /// </summary>
    public interface IConfigureEventObserverPipeline
    {
#if NET7_0_OR_GREATER
        static abstract void ConfigurePipeline(IEventObserverPipelineBuilder pipeline);
#endif
    }
}
