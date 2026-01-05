namespace Conqueror.Iterating;

using System.Collections;

internal sealed class IteratorPipeline<TIterator, TItem>(Type? handlerType, IServiceProvider serviceProvider)
    : IIteratorPipeline<TIterator, TItem>
    where TIterator : class, IIterator<TIterator, TItem>
{
    private readonly List<IIteratorMiddleware<TIterator, TItem>> middlewares = [];

    public Type? HandlerType { get; } = handlerType;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public int Count => middlewares.Count;

    public IIteratorPipeline<TIterator, TItem> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>
    {
        middlewares.Add(middleware);

        return this;
    }

    public IIteratorPipeline<TIterator, TItem> Use(IteratorMiddlewareFn<TIterator, TItem> middlewareFn) =>
        Use(new DelegateIteratorMiddleware(middlewareFn));

    public IIteratorPipeline<TIterator, TItem> UseWhen(
        Predicate<IteratorMiddlewareContext<TIterator, TItem>> predicate,
        Action<IIteratorPipeline<TIterator, TItem>> configureConditionalPipeline
    )
    {
        configureConditionalPipeline(new ConditionalPipeline(predicate, this));

        return this;
    }

    public IIteratorPipeline<TIterator, TItem> Without<TMiddleware>()
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>
    {
        _ = middlewares.RemoveAll(static m => m is TMiddleware or ConditionalIteratorMiddleware<TMiddleware>);

        return this;
    }

    public IIteratorPipeline<TIterator, TItem> Configure<TMiddleware>(Action<TMiddleware> configureFn)
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>
    {
        var found = false;
        foreach (var middleware in middlewares)
        {
            if (middleware is TMiddleware m)
            {
                configureFn(m);
                found = true;
            }

            if (middleware is ConditionalIteratorMiddleware<TMiddleware> conditionalMiddleware)
            {
                configureFn(conditionalMiddleware.Middleware);
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException(
                $"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used"
            );
        }

        return this;
    }

    public IEnumerator<IIteratorMiddleware<TIterator, TItem>> GetEnumerator() => middlewares.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IAsyncEnumerable<TItem> Execute(
        IServiceProvider serviceProvider,
        TIterator iterator,
        IIteratorClient<TIterator, TItem> client,
        IteratorTransportType transportType,
        ConquerorContext conquerorContext,
        CancellationToken cancellationToken
    )
    {
        if (middlewares.Count is 0)
        {
            return client.Execute(iterator, serviceProvider, conquerorContext, cancellationToken);
        }

        var ctx = new IteratorMiddlewareContext<TIterator, TItem>(
            middlewares,
            client,
            serviceProvider,
            conquerorContext,
            transportType
        )
        {
            Iterator = iterator,
            CancellationToken = cancellationToken,
        };

        return middlewares[0].Execute(ctx);
    }

    private sealed class DelegateIteratorMiddleware(IteratorMiddlewareFn<TIterator, TItem> middlewareFn)
        : IIteratorMiddleware<TIterator, TItem>
    {
        public IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx) => middlewareFn(ctx);
    }

    private sealed class ConditionalIteratorMiddleware<TMiddleware>(
        Predicate<IteratorMiddlewareContext<TIterator, TItem>> predicate,
        TMiddleware middleware
    ) : IIteratorMiddleware<TIterator, TItem>
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>
    {
        public TMiddleware Middleware => middleware;

        public IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx) =>
            predicate(ctx) ? middleware.Execute(ctx) : ctx.Next(ctx.Iterator, ctx.CancellationToken);
    }

    private sealed class ConditionalDelegateIteratorMiddleware(
        Predicate<IteratorMiddlewareContext<TIterator, TItem>> predicate,
        IteratorMiddlewareFn<TIterator, TItem> middlewareFn
    ) : IIteratorMiddleware<TIterator, TItem>
    {
        public IAsyncEnumerable<TItem> Execute(IteratorMiddlewareContext<TIterator, TItem> ctx) =>
            predicate(ctx) ? middlewareFn(ctx) : ctx.Next(ctx.Iterator, ctx.CancellationToken);
    }

    private sealed class ConditionalPipeline(
        Predicate<IteratorMiddlewareContext<TIterator, TItem>> outerPredicate,
        IIteratorPipeline<TIterator, TItem> pipeline
    ) : IIteratorPipeline<TIterator, TItem>
    {
        public Type? HandlerType => pipeline.HandlerType;

        public IServiceProvider ServiceProvider => pipeline.ServiceProvider;

        public int Count => pipeline.Count;

        public IIteratorPipeline<TIterator, TItem> Use<TMiddleware>(TMiddleware middleware)
            where TMiddleware : IIteratorMiddleware<TIterator, TItem>
        {
            _ = pipeline.Use(new ConditionalIteratorMiddleware<TMiddleware>(outerPredicate, middleware));

            return this;
        }

        public IIteratorPipeline<TIterator, TItem> Use(IteratorMiddlewareFn<TIterator, TItem> middlewareFn)
        {
            _ = pipeline.Use(new ConditionalDelegateIteratorMiddleware(outerPredicate, middlewareFn));

            return this;
        }

        public IIteratorPipeline<TIterator, TItem> UseWhen(
            Predicate<IteratorMiddlewareContext<TIterator, TItem>> predicate,
            Action<IIteratorPipeline<TIterator, TItem>> configureConditionalPipeline
        ) => pipeline.UseWhen(ctx => outerPredicate(ctx) && predicate(ctx), configureConditionalPipeline);

        public IIteratorPipeline<TIterator, TItem> Without<TMiddleware>()
            where TMiddleware : IIteratorMiddleware<TIterator, TItem> => pipeline.Without<TMiddleware>();

        public IIteratorPipeline<TIterator, TItem> Configure<TMiddleware>(Action<TMiddleware> configureFn)
            where TMiddleware : IIteratorMiddleware<TIterator, TItem> => pipeline.Configure(configureFn);

        IEnumerator<IIteratorMiddleware<TIterator, TItem>> IEnumerable<
            IIteratorMiddleware<TIterator, TItem>
        >.GetEnumerator() => pipeline.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)pipeline).GetEnumerator();
    }
}
