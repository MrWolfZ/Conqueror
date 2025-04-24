using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageSender<in TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    string TransportTypeName { get; }

    Task<TResponse> Send(TMessage message,
                         IServiceProvider serviceProvider,
                         ConquerorContext conquerorContext,
                         CancellationToken cancellationToken);
}
