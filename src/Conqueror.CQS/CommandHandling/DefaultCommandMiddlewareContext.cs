using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal delegate Task<TResponse> CommandMiddlewareNext<in TCommand, TResponse>(TCommand command, CancellationToken cancellationToken);

internal sealed class DefaultCommandMiddlewareContext<TCommand, TResponse> : CommandMiddlewareContext<TCommand, TResponse>
    where TCommand : class
{
    private readonly CommandMiddlewareNext<TCommand, TResponse> next;

    public DefaultCommandMiddlewareContext(TCommand command,
                                           CommandMiddlewareNext<TCommand, TResponse> next,
                                           IServiceProvider serviceProvider,
                                           IConquerorContext conquerorContext,
                                           CommandTransportType transportType,
                                           CancellationToken cancellationToken)
    {
        this.next = next;
        Command = command;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ConquerorContext = conquerorContext;
        TransportType = transportType;
    }

    public override TCommand Command { get; }

    public override bool HasUnitResponse { get; } = typeof(TResponse) == typeof(UnitCommandResponse);

    public override CancellationToken CancellationToken { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override IConquerorContext ConquerorContext { get; }

    public override CommandTransportType TransportType { get; }

    public override Task<TResponse> Next(TCommand command, CancellationToken cancellationToken) => next(command, cancellationToken);
}
