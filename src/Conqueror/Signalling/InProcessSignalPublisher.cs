using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Signalling;

internal sealed class InProcessSignalPublisher<TSignal>(
    ISignalBroadcastingStrategy broadcastingStrategy)
    : ISignalPublisher<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    public string TransportTypeName => ConquerorConstants.InProcessTransportName;

    public Task Publish(TSignal signal,
                        IServiceProvider serviceProvider,
                        ConquerorContext conquerorContext,
                        CancellationToken cancellationToken)
    {
        return serviceProvider.GetRequiredService<InProcessSignalReceiver>()
                              .Broadcast(signal, serviceProvider, broadcastingStrategy, cancellationToken);
    }
}
