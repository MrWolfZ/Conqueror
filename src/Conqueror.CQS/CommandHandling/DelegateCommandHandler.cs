using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.CommandHandling;

internal sealed class DelegateCommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : class
{
    private readonly Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn;
    private readonly IServiceProvider serviceProvider;

    public DelegateCommandHandler(Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
                                  IServiceProvider serviceProvider)
    {
        this.handlerFn = handlerFn;
        this.serviceProvider = serviceProvider;
    }

    public Task<TResponse> ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return handlerFn(command, serviceProvider, cancellationToken);
    }
}

internal sealed class DelegateCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : class
{
    private readonly Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn;
    private readonly IServiceProvider serviceProvider;

    public DelegateCommandHandler(Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn,
                                  IServiceProvider serviceProvider)
    {
        this.handlerFn = handlerFn;
        this.serviceProvider = serviceProvider;
    }

    public Task ExecuteCommand(TCommand command, CancellationToken cancellationToken = default)
    {
        return handlerFn(command, serviceProvider, cancellationToken);
    }
}
