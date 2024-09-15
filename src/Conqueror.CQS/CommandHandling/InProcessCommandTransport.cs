using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class InProcessCommandTransport(Type handlerType, Delegate? configurePipeline) : ICommandTransportClient
{
    public string TransportTypeName => InProcessCommandTransportTypeExtensions.TransportName;

    public Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
        where TCommand : class
    {
        var proxy = new CommandHandlerProxy<TCommand, TResponse>(serviceProvider,
                                                                 new(new HandlerInvoker(handlerType)),
                                                                 configurePipeline as Action<ICommandPipeline<TCommand, TResponse>>,
                                                                 CommandTransportRole.Server);

        return proxy.ExecuteCommand(command, cancellationToken);
    }

    private sealed class HandlerInvoker(Type handlerType) : ICommandTransportClient
    {
        public string TransportTypeName => InProcessCommandTransportTypeExtensions.TransportName;

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
}
