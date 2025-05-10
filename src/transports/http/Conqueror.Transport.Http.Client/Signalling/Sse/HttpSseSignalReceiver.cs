using System;

namespace Conqueror.Transport.Http.Client.Signalling.Sse;

internal sealed class HttpSseSignalReceiver<TSignal>(IServiceProvider serviceProvider) : IHttpSseSignalReceiver
    where TSignal : class, IHttpSseSignal<TSignal>
{
    public Type SignalType { get; } = typeof(TSignal);

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public bool IsEnabled => Configuration is not null;

    public HttpSseSignalReceiverConfiguration? Configuration { get; private set; }

    public IHttpSseSignalReceiver Disable()
    {
        Configuration = null;

        return this;
    }

    public HttpSseSignalReceiverConfiguration Enable(Uri address)
    {
        Configuration = new() { EventType = TSignal.EventType, Address = address };

        return Configuration;
    }
}
