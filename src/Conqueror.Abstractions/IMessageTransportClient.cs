using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IMessageTransportClient<in TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    string TransportTypeName { get; }

    Task<TResponse> Send(TMessage message,
                         IServiceProvider serviceProvider,
                         ConquerorContext conquerorContext,
                         CancellationToken cancellationToken);
}
