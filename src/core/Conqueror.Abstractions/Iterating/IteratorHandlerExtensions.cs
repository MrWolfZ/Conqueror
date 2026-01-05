namespace Conqueror;

public static class IteratorHandlerExtensions
{
    public static TIHandler WithPipeline<TIterator, TItem, TIHandler>(
        this IIteratorHandler<TIterator, TItem, TIHandler> handler,
        Action<IIteratorPipeline<TIterator, TItem>> configurePipeline
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
    {
        if (handler is IIteratorHandlerProxy<TIterator, TItem, TIHandler> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException(
            $"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}",
            nameof(handler),
            innerException: null
        );
    }

    public static TIHandler WithTransport<TIterator, TItem, TIHandler>(
        this IIteratorHandler<TIterator, TItem, TIHandler> handler,
        ConfigureIteratorClient<TIterator, TItem> configureTransport
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
    {
        if (handler is IIteratorHandlerProxy<TIterator, TItem, TIHandler> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException(
            $"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}",
            nameof(handler),
            innerException: null
        );
    }

    public static TIHandler WithTransport<TIterator, TItem, TIHandler>(
        this IIteratorHandler<TIterator, TItem, TIHandler> handler,
        ConfigureIteratorClientAsync<TIterator, TItem> configureTransport
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
    {
        if (handler is IIteratorHandlerProxy<TIterator, TItem, TIHandler> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException(
            $"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}",
            nameof(handler),
            innerException: null
        );
    }
}
