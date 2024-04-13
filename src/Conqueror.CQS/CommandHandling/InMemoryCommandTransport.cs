using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class InMemoryCommandTransport : ICommandTransportClient
{
    private readonly Type handlerType;

    public InMemoryCommandTransport(Type handlerType)
    {
        this.handlerType = handlerType;
    }

    public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command,
                                                                     IServiceProvider serviceProvider,
                                                                     CancellationToken cancellationToken)
        where TCommand : class
    {
        var handler = serviceProvider.GetRequiredService(handlerType);

        if (handler is ICommandHandler<TCommand, TResponse> h)
        {
            return await h.ExecuteCommand(command, cancellationToken).ConfigureAwait(false);
        }

        await ((ICommandHandler<TCommand>)handler).ExecuteCommand(command, cancellationToken).ConfigureAwait(false);
        return (TResponse)(object)UnitCommandResponse.Instance;
    }
}
