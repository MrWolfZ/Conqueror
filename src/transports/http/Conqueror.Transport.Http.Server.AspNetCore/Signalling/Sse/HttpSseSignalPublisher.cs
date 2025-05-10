using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed class HttpSseSignalPublisher<TSignal>(HttpSseSignalBroker broker) : IHttpSseSignalPublisher<TSignal>
    where TSignal : class, IHttpSseSignal<TSignal>
{
    public string TransportTypeName => ConquerorTransportHttpConstants.ServersSentEventsTransportName;

    public Task Publish(
        TSignal signal,
        IServiceProvider serviceProvider,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken)
    {
        return broker.Publish(signal, conquerorContext, cancellationToken);
    }
}
