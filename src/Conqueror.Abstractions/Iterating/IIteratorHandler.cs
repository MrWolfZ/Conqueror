#pragma warning disable SA1201 // ElementsMustAppearInTheCorrectOrder

namespace Conqueror;

using System.ComponentModel;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIteratorHandler
{
    /// <summary>
    ///     Implemented by source generator for each handler type. Cannot be abstract since otherwise
    ///     the generated <c>IHandler</c> types could not be used as generic arguments.
    /// </summary>
    /// <returns>The types injectors for all iterator types handled by this handler</returns>
    static virtual IEnumerable<IIteratorHandlerTypesInjector> GetTypeInjectors() =>
        throw new NotSupportedException(
            "this should be implemented by the source generator for each concrete handler type"
        );

    static virtual void ConfigureInProcessServer(IInProcessIteratorServer server)
    {
        // we don't configure the server (by default, it is enabled for all iterator types)
    }
}

/// <summary>
///     This interface is added to handler types by the source generator and purely
///     exists to cause a compiler error if the source generator is not correctly generating
///     the extensions for the handler type.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIteratorHandlerWithSourceGeneration;

[EditorBrowsable(EditorBrowsableState.Never)]
[SuppressMessage("ReSharper", "TypeParameterCanBeVariant", Justification = "false positive")]
public interface IIteratorHandler<TIterator, TItem, TIHandler> : IIteratorHandler
    where TIterator : class, IIterator<TIterator, TItem>
    where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
{
    static virtual IteratorTypes<TIterator, TItem, TIHandler> IteratorTypes { get; } = new();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IIteratorHandler<TIterator, TItem, TIHandler, TProxy, in TIPipeline, TPipelineProxy>
    : IIteratorHandler<TIterator, TItem, TIHandler>
    where TIterator : class, IIterator<TIterator, TItem>
    where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>
    where TProxy : IteratorHandlerProxy<TIterator, TItem, TIHandler>, TIHandler, new()
    where TIPipeline : class, IIteratorPipeline<TIterator, TItem>
    where TPipelineProxy : IteratorPipelineProxy<TIterator, TItem>, TIPipeline, new()
{
    static virtual void ConfigurePipeline(TIPipeline pipeline)
    {
        // by default, we use an empty pipeline
    }

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static IIteratorHandlerTypesInjector CreateCoreTypesInjector() =>
        new CoreIteratorHandlerTypesInjector<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>(
            configurePipeline: null,
            configureInProcessServer: null
        );

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    static IIteratorHandlerTypesInjector CreateCoreTypesInjector<THandler>()
        where THandler : class, TIHandler =>
        new CoreIteratorHandlerTypesInjector<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>(
            THandler.ConfigurePipeline,
            THandler.ConfigureInProcessServer
        );
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class IteratorHandlerProxy<TIterator, TItem, TIHandler>
    : IIteratorHandlerProxy<TIterator, TItem, TIHandler>
    where TIterator : class, IIterator<TIterator, TItem>
    where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
{
    [SuppressMessage(
        "Blocker Code Smell",
        "S3060:\"is\" should not be used with \"this\"",
        Justification = "the proxy should only be inherited by a specific source-generated type, and we need to assert this"
    )]
    protected IteratorHandlerProxy()
    {
        Debug.Assert(
            this is TIHandler,
            $"the proxy should implement {typeof(TIHandler).Name}, but it is {GetType()} instead"
        );

        This = (this as TIHandler)!;
    }

    // cannot be 'required' since that would block the `new()` constraint
    internal IServiceProvider ServiceProvider { get; init; } = null!;

    internal IIteratorDispatcher Dispatcher { get; init; } = null!;

    internal IIteratorPipeline<TIterator, TItem> Pipeline { get; init; } = null!;

    private IIteratorClient<TIterator, TItem>? Client { get; set; }

    [SuppressMessage(
        "Design",
        "MA0138:Do not use \'Async\' suffix when a method does not return an awaitable type",
        Justification = "false positive"
    )]
    private ConfigureIteratorClientAsync<TIterator, TItem>? ConfigureClientAsync { get; set; }

    private TIHandler This { get; }

    public TIHandler WithPipeline(Action<IIteratorPipeline<TIterator, TItem>> configurePipeline)
    {
        configurePipeline(Pipeline);

        return This;
    }

    public TIHandler WithTransport(ConfigureIteratorClient<TIterator, TItem> configureClient)
    {
        Client = configureClient(new(ServiceProvider));
        ConfigureClientAsync = null;

        return This;
    }

    public TIHandler WithTransport(ConfigureIteratorClientAsync<TIterator, TItem> configureClientAsync)
    {
        Client = null;
        ConfigureClientAsync = configureClientAsync;

        return This;
    }

    static IEnumerable<IIteratorHandlerTypesInjector> IIteratorHandler.GetTypeInjectors() =>
        throw new NotSupportedException("this method should never be called on the proxy");

    [EditorBrowsable(EditorBrowsableState.Never)]
    public IAsyncEnumerable<TItem> Handle(TIterator iterator, CancellationToken cancellationToken = default) =>
        Dispatcher.Dispatch(iterator, ServiceProvider, Pipeline, Client, ConfigureClientAsync, cancellationToken);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IIteratorHandlerProxy<TIterator, TItem, THandler> : IIteratorHandler<TIterator, TItem, THandler>
    where TIterator : class, IIterator<TIterator, TItem>
    where THandler : class, IIteratorHandler<TIterator, TItem, THandler>
{
    THandler WithPipeline(Action<IIteratorPipeline<TIterator, TItem>> configurePipeline);

    THandler WithTransport(ConfigureIteratorClient<TIterator, TItem> configureClient);

    THandler WithTransport(ConfigureIteratorClientAsync<TIterator, TItem> configureClientAsync);
}
