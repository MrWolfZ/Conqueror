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
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return builder.UseInProcessIfAvailable(transportTypeName)
               ?? throw new InvalidOperationException($"there is no handler registered for message type '{typeof(TMessage)}'");
    }

    public static IMessageTransportClient<TMessage, TResponse>? UseInProcessIfAvailable<TMessage, TResponse>(
        this IMessageTransportClientBuilder<TMessage, TResponse> builder,
        string? transportTypeName = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var registration = builder.ServiceProvider
                                  .GetRequiredService<MessageTransportRegistry>()
                                  .GetMessageHandlerRegistration(typeof(TMessage));

        if (registration is null)
        {
            return null;
        }

        return new InProcessMessageTransport<TMessage, TResponse>(registration.HandlerAdapterType ?? registration.HandlerType,
                                                                  registration.ConfigurePipeline,
                                                                  transportTypeName);
    }
}
