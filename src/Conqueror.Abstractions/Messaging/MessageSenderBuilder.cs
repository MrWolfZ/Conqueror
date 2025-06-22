using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public readonly record struct MessageSenderBuilder<TMessage, TResponse>(
    IServiceProvider ServiceProvider,
    ConquerorContext ConquerorContext)
    where TMessage : class, IMessage<TMessage, TResponse>;
