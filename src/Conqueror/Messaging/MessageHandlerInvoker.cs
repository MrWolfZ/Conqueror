using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageHandlerInvoker<TMessage, TResponse>(
    IConquerorContextAccessor conquerorContextAccessor,
    IMessageIdFactory messageIdFactory,
    Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline,
    MessageHandlerFn<TMessage, TResponse> handlerFn,
    Type? handlerType)
    : IMessageHandlerInvoker
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly MessageDispatcher dispatcher = new(
        conquerorContextAccessor,
        messageIdFactory,
        MessageTransportRole.Receiver,
        handlerType);

    public Task<TR> Invoke<TM, TR>(
        TM message,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken)
        where TM : class, IMessage<TM, TR>
    {
        Debug.Assert(typeof(TM) == typeof(TMessage), $"the message type was expected to be {typeof(TMessage)}, but was {typeof(TM)} instead.");
        Debug.Assert(typeof(TR) == typeof(TResponse), $"the response type was expected to be {typeof(TResponse)}, but was {typeof(TR)} instead.");

        return (Task<TR>)(object)dispatcher.Dispatch(
            (message as TMessage)!,
            serviceProvider,
            configurePipeline,
            new Sender(handlerFn, transportTypeName),
            configureSender: null,
            configureSenderAsync: null,
            cancellationToken);
    }

    private sealed class Sender(MessageHandlerFn<TMessage, TResponse> handlerFn, string transportTypeName) : IMessageSender<TMessage, TResponse>
    {
        public string TransportTypeName { get; } = transportTypeName;

        public Task<TResponse> Send(
            TMessage message,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken)
            => handlerFn(message, serviceProvider, cancellationToken);
    }
}

internal interface IMessageHandlerInvoker
{
    Task<TResponse> Invoke<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>;
}
