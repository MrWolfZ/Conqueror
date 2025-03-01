using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

/// <summary>
///     This class acts both as a publisher and transport client for in-memory events.
/// </summary>
internal sealed class InMemoryEventPublisher(
    IConquerorEventTransportClientRegistrar registrar,
    Action<IConquerorInMemoryEventPublishingStrategyBuilder> configureStrategy)
    : IConquerorEventTransportPublisher<InMemoryEventAttribute>, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1);

    private IConquerorEventTransportClientDispatcher? dispatcher;
    private ISet<ConquerorEventObserverId> observersToDispatchTo = new HashSet<ConquerorEventObserverId>();

    public async Task PublishEvent<TEvent>(TEvent evt, InMemoryEventAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
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

        if (dispatcher is not null)
        {
            await dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            await Initialize().ConfigureAwait(false);
            await PublishEvent(evt, configurationAttribute, serviceProvider, cancellationToken).ConfigureAwait(false);
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

    private async Task Initialize()
    {
        var registration = await registrar.RegisterTransportClient<InMemoryEventObserverTransportConfiguration, InMemoryEventAttribute>(configureStrategy).ConfigureAwait(false);
        observersToDispatchTo = registration.RelevantObservers.Select(r => r.ObserverId).ToHashSet();
        dispatcher = registration.Dispatcher;
    }
}
