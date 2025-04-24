using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class InProcessMessageSender<TMessage, TResponse>(
    IMessageReceiverHandlerInvoker invoker,
    string? transportTypeName)
    : IMessageSender<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public string TransportTypeName => transportTypeName ?? ConquerorConstants.InProcessTransportName;

    public Task<TResponse> Send(TMessage message,
                                      IServiceProvider serviceProvider,
                                      ConquerorContext conquerorContext,
                                      CancellationToken cancellationToken)
    {
        return invoker.Invoke<TMessage, TResponse>(message,
                                                   serviceProvider,
                                                   TransportTypeName,
                                                   cancellationToken);
    }
}
