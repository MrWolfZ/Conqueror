﻿using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Common;

namespace Conqueror.CQS.CommandHandling;

internal sealed class CommandWithoutResponseAdapter<TCommand> : ICommandHandler<TCommand>
    where TCommand : class
{
    private readonly ICommandHandler<TCommand, UnitCommandResponse> wrapped;

    public CommandWithoutResponseAdapter(ICommandHandler<TCommand, UnitCommandResponse> wrapped)
    {
        this.wrapped = wrapped;
    }

    public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken)
    {
        return wrapped.ExecuteCommand(command, cancellationToken);
    }
}