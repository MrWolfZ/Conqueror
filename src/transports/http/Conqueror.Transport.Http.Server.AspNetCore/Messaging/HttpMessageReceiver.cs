using System;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class HttpMessageReceiver<TMessage, TResponse>(Type? handlerType, IServiceProvider serviceProvider) : IHttpMessageReceiver
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public Type MessageType { get; } = typeof(TMessage);

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public bool IsEnabled { get; private set; } = true;

    public bool IsOmittedFromApiDescription { get; private set; }

    public void Disable()
    {
        IsEnabled = false;
    }

    public IHttpMessageReceiver OmitFromApiDescription()
    {
        IsOmittedFromApiDescription = true;
        return this;
    }
}
