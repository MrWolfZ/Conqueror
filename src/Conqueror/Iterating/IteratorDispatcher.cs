namespace Conqueror.Iterating;

using Context;

#pragma warning disable CS9113 // Parameter is unread (will be used when IteratorId is added to ConquerorContext)
internal sealed class IteratorDispatcher(
    IConquerorContextAccessor conquerorContextAccessor,
    IIteratorIdFactory iteratorIdFactory,
    IteratorTransportRole transportRole
) : IIteratorDispatcher
#pragma warning restore CS9113
{
    public IAsyncEnumerable<TItem> Dispatch<TIterator, TItem>(
        TIterator iterator,
        IServiceProvider serviceProvider,
        IIteratorPipeline<TIterator, TItem> pipeline,
        IIteratorClient<TIterator, TItem>? client,
        ConfigureIteratorClientAsync<TIterator, TItem>? configureClientAsync,
        CancellationToken cancellationToken
    )
        where TIterator : class, IIterator<TIterator, TItem>
    {
        if (pipeline is not IteratorPipeline<TIterator, TItem> concretePipeline)
        {
            throw new ArgumentException("pipeline must be a concrete pipeline", nameof(pipeline));
        }

        return DispatchInner(
            iterator,
            serviceProvider,
            concretePipeline,
            client,
            configureClientAsync,
            cancellationToken
        );
    }

    private async IAsyncEnumerable<TItem> DispatchInner<TIterator, TItem>(
        TIterator iterator,
        IServiceProvider serviceProvider,
        IteratorPipeline<TIterator, TItem> pipeline,
        IIteratorClient<TIterator, TItem>? client,
        ConfigureIteratorClientAsync<TIterator, TItem>? configureClientAsync,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
        where TIterator : class, IIterator<TIterator, TItem>
    {
        using var conquerorContext = conquerorContextAccessor.CloneOrCreate();
        var defaultConquerorContext = conquerorContext as DefaultConquerorContext;

        var originalIteratorId = conquerorContext.IteratorId;
        if (originalIteratorId is null || transportRole is IteratorTransportRole.Client)
        {
            conquerorContext.IteratorId = iteratorIdFactory.GenerateId();
        }

        if (client is null)
        {
            var transportBuilder = new IteratorClientBuilder<TIterator, TItem>(serviceProvider);

            if (configureClientAsync is not null)
            {
                client = await configureClientAsync(transportBuilder).ConfigureAwait(false);
            }
            else
            {
                client = transportBuilder.UseInProcess();
            }
        }

        var transportType = new IteratorTransportType(client.TransportTypeName, transportRole);

        await foreach (
            var item in pipeline
                .Execute(serviceProvider, iterator, client, transportType, conquerorContext, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            // given the multi-turn nature of async enumerables, we need to provide some extra handling for
            // the context data, since we need to propagate the context up per item instead of just at the end
            // of the iteration; it is an accepted trade-off that this only works with the default context
            // implementation since it is very unlikely that user's will provide their own context outside of
            // wanting to completely disable context handling for performance
            defaultConquerorContext?.PropagateUpstreamData();

            yield return item;

            // async enumerables do not preserve async locals correctly across yield points; therefore we have to
            // manually restore the correct context; it is an accepted trade-off that this only works with the default
            // context implementation since it is very unlikely that user's will provide their own context outside of
            // wanting to completely disable context handling for performance
            if (
                defaultConquerorContext is not null
                && conquerorContextAccessor is DefaultConquerorContextAccessor defaultAccessor
            )
            {
                defaultAccessor.Set(defaultConquerorContext);
            }
        }
    }
}
