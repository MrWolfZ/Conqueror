namespace Conqueror;

public static class InProcessIteratorClientBuilderExtensions
{
    public static IIteratorClient<TIterator, TItem> UseInProcess<TIterator, TItem>(
        this IteratorClientBuilder<TIterator, TItem> builder
    )
        where TIterator : class, IIterator<TIterator, TItem>
    {
        if (
            builder.ServiceProvider.GetService(typeof(IInProcessIteratorClientFactory))
            is not IInProcessIteratorClientFactory clientFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IInProcessIteratorClientFactory)}'; did you forget to add Conqueror to the service collection?"
            );
        }

        return clientFactory.Get<TIterator, TItem>();
    }

    public static IIteratorClient<TIterator, TItem>? UseInProcessIfAvailable<TIterator, TItem>(
        this IteratorClientBuilder<TIterator, TItem> builder
    )
        where TIterator : class, IIterator<TIterator, TItem>
    {
        if (
            builder.ServiceProvider.GetService(typeof(IInProcessIteratorClientFactory))
            is not IInProcessIteratorClientFactory clientFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IInProcessIteratorClientFactory)}'; did you forget to add Conqueror to the service collection?"
            );
        }

        return clientFactory.GetIfAvailable<TIterator, TItem>();
    }
}
