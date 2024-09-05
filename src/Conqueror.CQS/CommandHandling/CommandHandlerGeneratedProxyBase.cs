using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal abstract class CommandHandlerGeneratedProxyBase<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : class
{
    private readonly ICommandHandler<TCommand, TResponse> target;

    protected CommandHandlerGeneratedProxyBase(ICommandHandler<TCommand, TResponse> target)
    {
        this.target = target;
    }

    public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return target.ExecuteCommand(command, cancellationToken);
    }

    public ICommandHandler<TCommand, TResponse> WithPipeline(Action<ICommandPipelineBuilder> configure)
    {
        return target.WithPipeline(configure);
    }
}

internal abstract class CommandHandlerGeneratedProxyBase<TCommand> : ICommandHandler<TCommand>
    where TCommand : class
{
    private readonly ICommandHandler<TCommand> target;

    protected CommandHandlerGeneratedProxyBase(ICommandHandler<TCommand> target)
    {
        this.target = target;
    }

    public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return target.ExecuteCommand(command, cancellationToken);
    }

    public ICommandHandler<TCommand> WithPipeline(Action<ICommandPipelineBuilder> configure)
    {
        return target.WithPipeline(configure);
    }
}
