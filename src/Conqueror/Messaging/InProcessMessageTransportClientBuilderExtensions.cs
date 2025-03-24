using System;
using Conqueror.Messaging;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessMessageTransportClientBuilderExtensions
{
    public static IMessageTransportClient<TMessage, TResponse> UseInProcess<TMessage, TResponse>(
        this IMessageTransportClientBuilder<TMessage, TResponse> builder,
        string? transportTypeName = null)
        where TMessage : class, IMessage<TResponse>
    {
        var registration = builder.ServiceProvider
                                  .GetRequiredService<MessageTransportRegistry>()
                                  .GetMessageHandlerRegistration(typeof(TMessage));

        if (registration is null)
        {
            throw new InvalidOperationException($"there is no handler registered for message type '{typeof(TMessage)}'");
        }

        return new InProcessMessageTransport<TMessage, TResponse>(registration.HandlerType, registration.ConfigurePipeline, transportTypeName);
    }
}
