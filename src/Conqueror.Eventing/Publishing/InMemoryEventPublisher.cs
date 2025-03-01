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
    InMemoryEventPublisher.State state)
    : IConquerorEventTransportPublisher<InMemoryEventAttribute>, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1);

    public async Task PublishEvent<TEvent>(TEvent evt, InMemoryEventAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        // no need for strict thread-safe only-once initialization, since the registrar ensures
        // only-once initialization; the field is just a minor performance improvement in order
        // to only call the registrar when necessary
        if (state.Dispatcher is not null)
        {
            await state.Dispatcher.DispatchEvent(evt, state.ObserversToDispatchTo, serviceProvider, cancellationToken).ConfigureAwait(false);
            return;
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (state.Dispatcher is not null)
            {
                await state.Dispatcher.DispatchEvent(evt, state.ObserversToDispatchTo, serviceProvider, cancellationToken).ConfigureAwait(false);
                return;
            }

            await InitializeState().ConfigureAwait(false);
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

    private async Task InitializeState()
    {
        var registration = await registrar.RegisterTransportClient<InMemoryEventObserverTransportConfiguration, InMemoryEventAttribute>(state.ConfigureStrategy).ConfigureAwait(false);
        state.ObserversToDispatchTo = registration.RelevantObservers.Select(r => r.ObserverId).ToHashSet();
        state.Dispatcher = registration.Dispatcher;
    }

    internal sealed class State(Action<IConquerorInMemoryEventPublishingStrategyBuilder> configureStrategy)
    {
        public Action<IConquerorInMemoryEventPublishingStrategyBuilder> ConfigureStrategy { get; } = configureStrategy;

        public IConquerorEventTransportClientDispatcher? Dispatcher { get; set; }

        public ISet<ConquerorEventObserverId> ObserversToDispatchTo { get; set; } = new HashSet<ConquerorEventObserverId>();
    }
}
