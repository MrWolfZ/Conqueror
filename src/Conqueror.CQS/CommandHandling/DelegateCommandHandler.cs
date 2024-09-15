using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class DelegateCommandHandler<TCommand, TResponse>(
    Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
    IServiceProvider serviceProvider)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : class
{
    public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return handlerFn(command, serviceProvider, cancellationToken);
    }
}

internal sealed class DelegateCommandHandler<TCommand>(
    Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn,
    IServiceProvider serviceProvider)
    : ICommandHandler<TCommand>
    where TCommand : class
{
    public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return handlerFn(command, serviceProvider, cancellationToken);
    }
}
