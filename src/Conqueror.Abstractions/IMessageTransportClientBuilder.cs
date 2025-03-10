using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Conqueror;

public delegate IMessageTransportClient ConfigureMessageTransportClient<TMessage, TResponse>(IMessageTransportClientBuilder<TMessage, TResponse> builder)
    where TMessage : class, IMessage<TResponse>;

public delegate Task<IMessageTransportClient> ConfigureMessageTransportClientAsync<TMessage, TResponse>(IMessageTransportClientBuilder<TMessage, TResponse> builder)
    where TMessage : class, IMessage<TResponse>;

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "The parameter is used by extension methods to get the message type")]
public interface IMessageTransportClientBuilder<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }
}
