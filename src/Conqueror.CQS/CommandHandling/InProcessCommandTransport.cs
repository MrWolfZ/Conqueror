using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling;

internal sealed class InProcessCommandTransport(Type handlerType, Delegate? configurePipeline) : ICommandTransportClient
{
    public string TransportTypeName => InProcessCommandTransportTypeExtensions.TransportName;

    public Task<TResponse> Send<TCommand, TResponse>(TCommand command,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
        where TCommand : class
    {
        var proxy = new CommandHandlerProxy<TCommand, TResponse>(serviceProvider,
                                                                 new(new HandlerInvoker(handlerType)),
                                                                 configurePipeline as Action<ICommandPipeline<TCommand, TResponse>>,
                                                                 CommandTransportRole.Server);

        return proxy.Handle(command, cancellationToken);
    }

    private sealed class HandlerInvoker(Type handlerType) : ICommandTransportClient
    {
        public string TransportTypeName => InProcessCommandTransportTypeExtensions.TransportName;

        public async Task<TResponse> Send<TCommand, TResponse>(TCommand command,
                                                                         IServiceProvider serviceProvider,
                                                                         CancellationToken cancellationToken)
            where TCommand : class
        {
            var handler = serviceProvider.GetRequiredService(handlerType);

            if (handler is ICommandHandler<TCommand, TResponse> h)
            {
                return await h.Handle(command, cancellationToken).ConfigureAwait(false);
            }

            await ((ICommandHandler<TCommand>)handler).Handle(command, cancellationToken).ConfigureAwait(false);
            return (TResponse)(object)UnitCommandResponse.Instance;
        }
    }
}
