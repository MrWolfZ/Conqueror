using System;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalPublisherFactory<TSignal>
    where TSignal : class, ISignal<TSignal>
{
    private readonly ConfigureSignalPublisherAsync<TSignal>? asyncPublisherFactory;
    private readonly ISignalPublisher<TSignal>? publisher;
    private readonly ConfigureSignalPublisher<TSignal>? syncPublisherFactory;

    public SignalPublisherFactory(ISignalPublisher<TSignal> publisher)
    {
        this.publisher = publisher;
    }

    public SignalPublisherFactory(ConfigureSignalPublisher<TSignal>? syncPublisherFactory)
    {
        this.syncPublisherFactory = syncPublisherFactory;
    }

    public SignalPublisherFactory(ConfigureSignalPublisherAsync<TSignal>? asyncPublisherFactory)
    {
        this.asyncPublisherFactory = asyncPublisherFactory;
    }

    public Task<ISignalPublisher<TSignal>> Create(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (publisher is not null)
        {
            return Task.FromResult(publisher);
        }

        var publisherBuilder = new SignalPublisherBuilder<TSignal>(serviceProvider, conquerorContext);

        if (syncPublisherFactory is not null)
        {
            return Task.FromResult(syncPublisherFactory.Invoke(publisherBuilder));
        }

        if (asyncPublisherFactory is not null)
        {
            return asyncPublisherFactory.Invoke(publisherBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create publisher for signal type '{typeof(TSignal)}' since it was not configured with a factory");
    }
}
