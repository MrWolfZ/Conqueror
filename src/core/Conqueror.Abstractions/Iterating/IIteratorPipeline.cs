#pragma warning disable CA1034

namespace Conqueror;

using System.Collections;
using System.ComponentModel;

public delegate IAsyncEnumerable<TItem> IteratorMiddlewareFn<TIterator, TItem>(
    IteratorMiddlewareContext<TIterator, TItem> context
)
    where TIterator : class, IIterator<TIterator, TItem>;

[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "naming is intentional")]
public interface IIteratorPipeline<TIterator, TItem> : IReadOnlyCollection<IIteratorMiddleware<TIterator, TItem>>
    where TIterator : class, IIterator<TIterator, TItem>
{
    /// <summary>
    ///     The type of the handler this pipeline is being built for. Is <see langword="null" /> for
    ///     delegate handlers or when the pipeline is being built for an iterator caller.
    /// </summary>
    Type? HandlerType { get; }

    IServiceProvider ServiceProvider { get; }

    IIteratorPipeline<TIterator, TItem> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>;

    IIteratorPipeline<TIterator, TItem> Use(IteratorMiddlewareFn<TIterator, TItem> middlewareFn);

    IIteratorPipeline<TIterator, TItem> UseWhen(
        Predicate<IteratorMiddlewareContext<TIterator, TItem>> predicate,
        Action<IIteratorPipeline<TIterator, TItem>> configureConditionalPipeline
    );

    IIteratorPipeline<TIterator, TItem> Without<TMiddleware>()
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>;

    IIteratorPipeline<TIterator, TItem> Configure<TMiddleware>(Action<TMiddleware> configureFn)
        where TMiddleware : IIteratorMiddleware<TIterator, TItem>;
}

[EditorBrowsable(EditorBrowsableState.Never)]
public class IteratorPipelineProxy<TIterator, TItem> : IIteratorPipeline<TIterator, TItem>
    where TIterator : class, IIterator<TIterator, TItem>
{
    public int Count => Wrapped.Count;

    public Type? HandlerType => Wrapped.HandlerType;

    public IServiceProvider ServiceProvider => Wrapped.ServiceProvider;

    internal IIteratorPipeline<TIterator, TItem> Wrapped { get; init; } = null!; // guaranteed to be set in init code

    IEnumerator<IIteratorMiddleware<TIterator, TItem>> IEnumerable<
        IIteratorMiddleware<TIterator, TItem>
    >.GetEnumerator() => Wrapped.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Wrapped).GetEnumerator();

    public IIteratorPipeline<TIterator, TItem> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IIteratorMiddleware<TIterator, TItem> => Wrapped.Use(middleware);

    public IIteratorPipeline<TIterator, TItem> Use(IteratorMiddlewareFn<TIterator, TItem> middlewareFn) =>
        Wrapped.Use(middlewareFn);

    public IIteratorPipeline<TIterator, TItem> UseWhen(
        Predicate<IteratorMiddlewareContext<TIterator, TItem>> predicate,
        Action<IIteratorPipeline<TIterator, TItem>> configureConditionalPipeline
    ) => Wrapped.UseWhen(predicate, configureConditionalPipeline);

    public IIteratorPipeline<TIterator, TItem> Without<TMiddleware>()
        where TMiddleware : IIteratorMiddleware<TIterator, TItem> => Wrapped.Without<TMiddleware>();

    public IIteratorPipeline<TIterator, TItem> Configure<TMiddleware>(Action<TMiddleware> configureFn)
        where TMiddleware : IIteratorMiddleware<TIterator, TItem> => Wrapped.Configure(configureFn);
}
