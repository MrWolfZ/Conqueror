using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class CommandMiddlewareContext<TCommand, TResponse>
    where TCommand : class
{
    public abstract TCommand Command { get; }

    public abstract bool HasUnitResponse { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract IConquerorContext ConquerorContext { get; }

    public abstract CommandTransportType TransportType { get; }

    public abstract Task<TResponse> Next(TCommand command, CancellationToken cancellationToken);
}
