using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct MessageMiddlewareContext<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly IMessageMiddleware<TMessage, TResponse>[] middlewares;
    private readonly IMessageSender<TMessage, TResponse> sender;
    private readonly int count;

    public MessageMiddlewareContext(
        IMessageMiddleware<TMessage, TResponse>[] middlewares,
        int count,
        IMessageSender<TMessage, TResponse> sender)
    {
        Debug.Assert(middlewares.Length > 0, "this should only be called if there are middlewares to execute");

        this.middlewares = middlewares;
        this.sender = sender;
        this.count = count;
    }

    public required TMessage Message { get; init; }

    public bool HasUnitResponse => typeof(TResponse) == typeof(UnitMessageResponse);

    public required CancellationToken CancellationToken { get; init; }

    public required IServiceProvider ServiceProvider { get; init; }

    public required ConquerorContext ConquerorContext { get; init; }

    public required MessageTransportType TransportType { get; init; }

    private int CurrentIndex { get; init; }

    public Task<TResponse> Next(TMessage message, CancellationToken cancellationToken)
    {
        var nextIndex = CurrentIndex + 1;
        if (nextIndex < count)
        {
            var updatedContext = this with
            {
                Message = message,
                CancellationToken = cancellationToken,
                CurrentIndex = nextIndex,
            };

            return middlewares[nextIndex].Execute(updatedContext);
        }

        return sender.Send(message, ServiceProvider, ConquerorContext, cancellationToken);
    }
}
