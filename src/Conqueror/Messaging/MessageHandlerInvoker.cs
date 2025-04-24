using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageHandlerInvoker<TMessage, TResponse>(
    Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline,
    MessageHandlerFn<TMessage, TResponse> handlerFn)
    : IMessageHandlerInvoker
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public async Task<TR> Invoke<TM, TR>(TM message, IServiceProvider serviceProvider, string transportTypeName, CancellationToken cancellationToken)
        where TM : class, IMessage<TM, TR>
    {
        Debug.Assert(typeof(TM) == typeof(TMessage), $"the signal type was expected to be {typeof(TMessage)}, but was {typeof(TM)} instead.");
        Debug.Assert(typeof(TR) == typeof(TResponse), $"the signal type was expected to be {typeof(TResponse)}, but was {typeof(TR)} instead.");

        var dispatcher = new MessageDispatcher<TMessage, TResponse>(serviceProvider,
                                                                    new(new Sender(handlerFn, transportTypeName)),
                                                                    configurePipeline,
                                                                    MessageTransportRole.Receiver);

        var response = await dispatcher.Dispatch((message as TMessage)!, cancellationToken).ConfigureAwait(false);
        return (TR)(object)response!;
    }

    private sealed class Sender(MessageHandlerFn<TMessage, TResponse> handlerFn, string transportTypeName) : IMessageSender<TMessage, TResponse>
    {
        public string TransportTypeName { get; } = transportTypeName;

        public Task<TResponse> Send(TMessage message, IServiceProvider serviceProvider, ConquerorContext conquerorContext, CancellationToken cancellationToken)
            => handlerFn(message, serviceProvider, cancellationToken);
    }
}

internal interface IMessageHandlerInvoker
{
    Task<TResponse> Invoke<TMessage, TResponse>(TMessage message,
                                                IServiceProvider serviceProvider,
                                                string transportTypeName,
                                                CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>;
}
