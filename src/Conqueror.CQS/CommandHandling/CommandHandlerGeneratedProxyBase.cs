using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal abstract class CommandHandlerGeneratedProxyBase<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> target)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : class
{
    public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return target.ExecuteCommand(command, cancellationToken);
    }

    public ICommandHandler<TCommand, TResponse> WithPipeline(Action<ICommandPipeline<TCommand, TResponse>> configure)
    {
        return target.WithPipeline(configure);
    }
}

internal abstract class CommandHandlerGeneratedProxyBase<TCommand>(ICommandHandler<TCommand> target) : ICommandHandler<TCommand>
    where TCommand : class
{
    public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return target.ExecuteCommand(command, cancellationToken);
    }

    public ICommandHandler<TCommand> WithPipeline(Action<ICommandPipeline<TCommand>> configure)
    {
        return target.WithPipeline(configure);
    }
}
