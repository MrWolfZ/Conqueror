namespace Conqueror.Iterating;

internal sealed class InProcessIteratorClientFactory(IServiceProvider serviceProvider, IteratorHandlerRegistry registry)
    : IInProcessIteratorClientFactory
{
    private readonly ConcurrentDictionary<Type, object?> clientByIteratorType = [];

    public IIteratorClient<TIterator, TItem> Get<TIterator, TItem>()
        where TIterator : class, IIterator<TIterator, TItem>
    {
        var (handler, isDisabled) = GetInternal<TIterator, TItem>();

        if (isDisabled)
        {
            throw new InvalidOperationException(
                $"in-process transport is disabled for iterator type '{typeof(TIterator)}'"
            );
        }

        return handler
               ?? throw new InvalidOperationException(
                   $"there is no handler registered for iterator type '{typeof(TIterator)}'"
               );
    }

    public IIteratorClient<TIterator, TItem>? GetIfAvailable<TIterator, TItem>()
        where TIterator : class, IIterator<TIterator, TItem>
    {
        var (handler, _) = GetInternal<TIterator, TItem>();

        return handler;
    }

    private (IIteratorClient<TIterator, TItem>? Handler, bool IsDisabled) GetInternal<TIterator, TItem>()
        where TIterator : class, IIterator<TIterator, TItem>
    {
        if (clientByIteratorType.TryGetValue(typeof(TIterator), out var client))
        {
            return client is IIteratorClient<TIterator, TItem> c ? (c, false) : (null, true);
        }

        var invoker = registry.GetServerHandlerInvoker<TIterator, TItem, ICoreIteratorHandlerTypesInjector>();

        if (invoker is null)
        {
            return (null, false);
        }

        var server = new InProcessIteratorServer<TIterator, TItem>(serviceProvider);
        invoker.TypesInjector.ConfigureInProcessServer(server);

        if (!server.IsEnabled)
        {
            if (!server.MustBeConfiguredOnEveryIteration)
            {
                clientByIteratorType[typeof(TIterator)] = null;
            }

            return (null, true);
        }

        var newClient = new InProcessIteratorClient<TIterator, TItem>(invoker);

        if (!server.MustBeConfiguredOnEveryIteration)
        {
            clientByIteratorType[typeof(TIterator)] = newClient;
        }

        return (newClient, false);
    }
}
