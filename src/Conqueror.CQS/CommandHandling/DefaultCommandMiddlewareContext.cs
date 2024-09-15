using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal delegate Task<TResponse> CommandMiddlewareNext<in TCommand, TResponse>(TCommand command, CancellationToken cancellationToken);

internal sealed class DefaultCommandMiddlewareContext<TCommand, TResponse>(
    TCommand command,
    CommandMiddlewareNext<TCommand, TResponse> next,
    IServiceProvider serviceProvider,
    IConquerorContext conquerorContext,
    CommandTransportType transportType,
    CancellationToken cancellationToken)
    : CommandMiddlewareContext<TCommand, TResponse>
    where TCommand : class
{
    public override TCommand Command { get; } = command;

    public override bool HasUnitResponse { get; } = typeof(TResponse) == typeof(UnitCommandResponse);

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override IConquerorContext ConquerorContext { get; } = conquerorContext;

    public override CommandTransportType TransportType { get; } = transportType;

    public override Task<TResponse> Next(TCommand command, CancellationToken cancellationToken) => next(command, cancellationToken);
}
