using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

internal interface IMessageDispatcher
{
    Task<TResponse> Dispatch<TMessage, TResponse>(
        TMessage message,
        IServiceProvider serviceProvider,
        Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline,
        IMessageSender<TMessage, TResponse>? sender,
        ConfigureMessageSender<TMessage, TResponse>? configureSender,
        ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync,
        CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>;
}
