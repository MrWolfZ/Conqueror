using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerWithoutResponseAdapter<TCommand> : ICommandHandler<TCommand>
    where TCommand : class
{
    private readonly ICommandHandler<TCommand, UnitCommandResponse> wrapped;

    public CommandHandlerWithoutResponseAdapter(ICommandHandler<TCommand, UnitCommandResponse> wrapped)
    {
        this.wrapped = wrapped;
    }

    public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return wrapped.ExecuteCommand(command, cancellationToken);
    }

    public ICommandHandler<TCommand> WithPipeline(Action<ICommandPipeline<TCommand>> configure)
    {
        return new CommandHandlerWithoutResponseAdapter<TCommand>(wrapped.WithPipeline(p => configure(new CommandPipelineWithoutResponseAdapter<TCommand>(p))));
    }
}
