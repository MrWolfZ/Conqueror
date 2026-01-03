#pragma warning disable SA1201 // ElementsMustAppearInTheCorrectOrder

namespace Conqueror;

using System.ComponentModel;

/// <summary>
///     Base interface for transports to be able to get an injector that works
///     with their specific constraint interface.
/// </summary>
public interface IIteratorHandlerTypesInjector
{
    Type IteratorType { get; }
}

internal interface ICoreIteratorHandlerTypesInjector : IIteratorHandlerTypesInjector
{
    Delegate? ConfigurePipeline { get; }

    void ConfigureInProcessServer(IInProcessIteratorServer server);

    /// <summary>
    ///     Helper method to be able to access the iterator types as generic parameters while only
    ///     having a generic reference to the iterator or handler type. This allows bypassing reflection.
    /// </summary>
    /// <param name="injectable">The injectable that should be called with the generic type parameters</param>
    /// <param name="arg">The argument to pass to the injectable</param>
    /// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
    /// <typeparam name="TResult">The type of result the injectable will return</typeparam>
    /// <returns>The result of calling the injectable</returns>
    TResult Inject<TArg, TResult>(ICoreIteratorHandlerTypesInjectable<TArg, TResult> injectable, TArg arg);
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CoreIteratorHandlerTypesInjector<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>(
    Delegate? configurePipeline,
    Action<IInProcessIteratorServer>? configureInProcessServer
) : ICoreIteratorHandlerTypesInjector
    where TIterator : class, IIterator<TIterator, TItem>
    where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>
    where TProxy : IteratorHandlerProxy<TIterator, TItem, TIHandler>, TIHandler, new()
    where TIPipeline : class, IIteratorPipeline<TIterator, TItem>
    where TPipelineProxy : IteratorPipelineProxy<TIterator, TItem>, TIPipeline, new()
{
    public Type IteratorType { get; } = typeof(TIterator);

    public Delegate? ConfigurePipeline { get; } = configurePipeline;

    public void ConfigureInProcessServer(IInProcessIteratorServer server)
    {
        // will be null for delegate handlers
        configureInProcessServer?.Invoke(server);
    }

    public TResult Inject<TArg, TResult>(ICoreIteratorHandlerTypesInjectable<TArg, TResult> injectable, TArg arg) =>
        injectable.WithInjectedTypes<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>(arg);
}

/// <summary>
///     Helper interface to be able to access the iterator types as generic parameters while only
///     having a generic reference to an iterator handler type. This allows bypassing reflection.
/// </summary>
/// <typeparam name="TArg">Type of the argument that will be passed to the injectable</typeparam>
/// <typeparam name="TResult">The type of result the injectable will return</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
internal interface ICoreIteratorHandlerTypesInjectable<in TArg, out TResult>
{
    TResult WithInjectedTypes<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>(TArg arg)
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>
        where TProxy : IteratorHandlerProxy<TIterator, TItem, TIHandler>, TIHandler, new()
        where TIPipeline : class, IIteratorPipeline<TIterator, TItem>
        where TPipelineProxy : IteratorPipelineProxy<TIterator, TItem>, TIPipeline, new();
}
