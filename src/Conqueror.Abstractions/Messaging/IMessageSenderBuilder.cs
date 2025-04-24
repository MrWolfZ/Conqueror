using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageSenderBuilder<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    IServiceProvider ServiceProvider { get; }

    ConquerorContext ConquerorContext { get; }
}
