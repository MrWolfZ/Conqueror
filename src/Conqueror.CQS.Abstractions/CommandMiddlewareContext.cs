using System.Threading;
using System.Threading.Tasks;

// these classes belong together
#pragma warning disable SA1402

namespace Conqueror
{
    public abstract class CommandMiddlewareContext<TCommand, TResponse>
        where TCommand : class
    {
        public abstract TCommand Command { get; }

        public abstract bool HasUnitResponse { get; }

        public abstract CancellationToken CancellationToken { get; }

        public abstract Task<TResponse> Next(TCommand command, CancellationToken cancellationToken);
    }

    public abstract class CommandMiddlewareContext<TCommand, TResponse, TConfiguration> : CommandMiddlewareContext<TCommand, TResponse>
        where TCommand : class
    {
        public abstract TConfiguration Configuration { get; }
    }
}
