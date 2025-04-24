using System;

namespace Conqueror.Transport.Http.Server.AspNetCore.Messaging;

internal sealed class HttpMessageReceiver<TMessage, TResponse>(IServiceProvider serviceProvider) : IHttpMessageReceiver
    where TMessage : class, IHttpMessage<TMessage, TResponse>
{
    public Type MessageType { get; } = typeof(TMessage);

    public IServiceProvider ServiceProvider { get; } = serviceProvider;
    public bool IsEnabled { get; private set; } = true;

    public bool IsOmittedFromApiDescription { get; private set; }

    public IHttpMessageReceiver Enable()
    {
        IsEnabled = true;
        return this;
    }

    public IHttpMessageReceiver Disable()
    {
        IsEnabled = false;
        return this;
    }

    public IHttpMessageReceiver OmitFromApiDescription()
    {
        IsOmittedFromApiDescription = true;
        return this;
    }
}
