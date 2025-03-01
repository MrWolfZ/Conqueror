using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Observing;

/// <summary>
///     This class acts both as a publisher and transport client for in-memory events.
/// </summary>
internal sealed class InProcessEventTransportReceiver(
    IConquerorEventTransportReceiverRegistry registry,
    InProcessEventTransportReceiver.Configuration? configuration = null)
    : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1);

    private IConquerorEventTransportReceiverDispatcher? dispatcher;

    private ISet<ConquerorEventObserverId> observersToDispatchTo = new HashSet<ConquerorEventObserverId>();

    public async Task Handle<TEvent>(TEvent evt, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        // no need for strict thread-safe only-once initialization, since the registrar ensures
        // only-once initialization; the field is just a minor performance improvement in order
        // to only call the registrar when necessary
        if (dispatcher is not null)
        {
            await dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider, cancellationToken).ConfigureAwait(false);
            return;
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (dispatcher is not null)
            {
                await dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider, cancellationToken).ConfigureAwait(false);
                return;
            }

            await InitializeState().ConfigureAwait(false);
            await Handle(evt, serviceProvider, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private async Task InitializeState()
    {
        var registration = await registry.RegisterReceiver<InMemoryEventObserverTransportConfiguration, InMemoryEventAttribute>(
                                             configuration?.ConfigureStrategy ?? (_ => { }))
                                         .ConfigureAwait(false);

        observersToDispatchTo = registration.RelevantObservers.Select(r => r.ObserverId).ToHashSet();
        dispatcher = registration.Dispatcher;
    }

    internal sealed record Configuration(Action<IConquerorEventBroadcastingStrategyBuilder> ConfigureStrategy);
}
