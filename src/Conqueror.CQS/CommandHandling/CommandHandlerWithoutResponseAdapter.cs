using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandHandlerWithoutResponseAdapter<TCommand>(ICommandHandler<TCommand, UnitCommandResponse> wrapped) : ICommandHandler<TCommand>
    where TCommand : class
{
    public Task Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        return wrapped.Handle(command, cancellationToken);
    }

    public ICommandHandler<TCommand> WithPipeline(Action<ICommandPipeline<TCommand>> configure)
    {
        return new CommandHandlerWithoutResponseAdapter<TCommand>(wrapped.WithPipeline(p => configure(new CommandPipelineWithoutResponseAdapter<TCommand>(p))));
    }
}
