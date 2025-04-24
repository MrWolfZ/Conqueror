using System;
using Conqueror.Messaging;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessMessageSenderBuilderExtensions
{
    public static IMessageSender<TMessage, TResponse> UseInProcess<TMessage, TResponse>(
        this IMessageSenderBuilder<TMessage, TResponse> builder,
        string? transportTypeName = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var (handler, isDisabled) = builder.UseInProcessInternal(transportTypeName);

        if (isDisabled)
        {
            throw new InvalidOperationException($"in-process transport is disabled for message type '{typeof(TMessage)}'");
        }

        return handler ?? throw new InvalidOperationException($"there is no handler registered for message type '{typeof(TMessage)}'");
    }

    public static IMessageSender<TMessage, TResponse>? UseInProcessIfAvailable<TMessage, TResponse>(
        this IMessageSenderBuilder<TMessage, TResponse> builder,
        string? transportTypeName = null)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var (handler, _) = builder.UseInProcessInternal(transportTypeName);
        return handler;
    }

    private static (IMessageSender<TMessage, TResponse>? Handler, bool IsDisabled) UseInProcessInternal<TMessage, TResponse>(
        this IMessageSenderBuilder<TMessage, TResponse> builder,
        string? transportTypeName)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        var invoker = builder.ServiceProvider
                             .GetRequiredService<MessageHandlerRegistry>()
                             .GetReceiverHandlerInvoker<TMessage, TResponse, ICoreMessageHandlerTypesInjector>();

        if (invoker is null)
        {
            return (null, false);
        }

        var isEnabled = invoker.TypesInjector.Create(new Injectable(builder.ServiceProvider));

        if (!isEnabled)
        {
            return (null, true);
        }

        return (new InProcessMessageSender<TMessage, TResponse>(invoker, transportTypeName), false);
    }

    private sealed class Injectable(IServiceProvider serviceProvider) : ICoreMessageHandlerTypesInjectable<bool>
    {
        bool ICoreMessageHandlerTypesInjectable<bool>.WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler>()
        {
            var receiver = new InProcessMessageReceiver<TMessage, TResponse>(serviceProvider);
            THandler.ConfigureInProcessReceiver(receiver);
            return receiver.IsEnabled;
        }
    }
}
