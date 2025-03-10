using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class MessageMiddlewareContext<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    public abstract TMessage Message { get; }

    public abstract bool HasUnitResponse { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract MessageTransportType TransportType { get; }

    public abstract Task<TResponse> Next(TMessage message, CancellationToken cancellationToken);
}
