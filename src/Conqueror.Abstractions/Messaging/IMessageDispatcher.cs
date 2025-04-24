using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal interface IMessageDispatcher<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    Task<TResponse> Dispatch(TMessage message, CancellationToken cancellationToken);

    IMessageDispatcher<TMessage, TResponse> WithPipeline(Action<IMessagePipeline<TMessage, TResponse>> configurePipeline);

    IMessageDispatcher<TMessage, TResponse> WithSender(ConfigureMessageSender<TMessage, TResponse> configureSender);

    IMessageDispatcher<TMessage, TResponse> WithSender(ConfigureMessageSenderAsync<TMessage, TResponse> configureSender);
}
