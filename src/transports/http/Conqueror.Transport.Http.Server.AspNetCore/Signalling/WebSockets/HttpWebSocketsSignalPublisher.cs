using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalPublisher<TSignal> : IHttpWebSocketsSignalPublisher<TSignal>
    where TSignal : class, IHttpWebSocketsSignal<TSignal>
{
    public static readonly HttpWebSocketsSignalPublisher<TSignal> Instance = new();

    public string TransportTypeName => WebSocketsTransportName;

    public Task Publish(
        TSignal signal,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        return serviceProvider.GetRequiredService<HttpWebSocketsSignalBroker>()
                              .Publish(signal, conquerorContext, cancellationToken);
    }
}
