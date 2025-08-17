namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed class HttpSseSignalPublisher<TSignal> : IHttpSseSignalPublisher<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    public static readonly HttpSseSignalPublisher<TSignal> Instance = new();

    public string TransportTypeName => ServersSentEventsTransportName;

    public Task Publish(
        TSignal signal,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
    {
        return serviceProvider
            .GetRequiredService<HttpSseSignalBroker>()
            .Publish(signal, conquerorContext, cancellationToken);
    }
}
