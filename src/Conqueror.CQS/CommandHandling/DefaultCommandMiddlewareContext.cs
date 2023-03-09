using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal delegate Task<TResponse> CommandMiddlewareNext<in TCommand, TResponse>(TCommand command, CancellationToken cancellationToken);

internal sealed class DefaultCommandMiddlewareContext<TCommand, TResponse, TConfiguration> : CommandMiddlewareContext<TCommand, TResponse, TConfiguration>
    where TCommand : class
{
    private readonly CommandMiddlewareNext<TCommand, TResponse> next;

    public DefaultCommandMiddlewareContext(TCommand command,
                                           CommandMiddlewareNext<TCommand, TResponse> next,
                                           TConfiguration configuration,
                                           IServiceProvider serviceProvider,
                                           IConquerorContext conquerorContext,
                                           CancellationToken cancellationToken)
    {
        this.next = next;
        Command = command;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ConquerorContext = conquerorContext;
        Configuration = configuration;
    }

    public override TCommand Command { get; }

    public override bool HasUnitResponse { get; } = typeof(TResponse) == typeof(UnitCommandResponse);

    public override CancellationToken CancellationToken { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override IConquerorContext ConquerorContext { get; }

    public override TConfiguration Configuration { get; }

    public override Task<TResponse> Next(TCommand command, CancellationToken cancellationToken) => next(command, cancellationToken);
}
