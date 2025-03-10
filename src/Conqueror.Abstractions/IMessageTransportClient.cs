using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public interface IMessageTransportClient
{
    string TransportTypeName { get; }

    Task<TResponse> Send<TMessage, TResponse>(TMessage message,
                                              IServiceProvider serviceProvider,
                                              CancellationToken cancellationToken)
        where TMessage : class, IMessage<TResponse>;
}
