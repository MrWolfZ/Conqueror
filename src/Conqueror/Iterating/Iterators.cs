namespace Conqueror.Iterating;

internal sealed class Iterators(IServiceProvider serviceProvider, IIteratorDispatcher dispatcher) : IIterators
{
    private static readonly Injectable HandlerCreationInjectable = new();

    public TIHandler For<TIterator, TItem, TIHandler>(IteratorTypes<TIterator, TItem, TIHandler> iteratorTypes)
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
    {
        var proxy = ((ICoreIteratorHandlerTypesInjector)TIterator.CoreTypesInjector).Inject(
            HandlerCreationInjectable,
            new(serviceProvider, dispatcher)
        );

        Debug.Assert(
            proxy is TIHandler,
            $"handler proxy was not of correct type; expected handler type '{typeof(TIHandler)}', actual '{proxy.GetType()}'"
        );

        return (TIHandler)proxy;
    }

    private readonly record struct InjectableArg(IServiceProvider ServiceProvider, IIteratorDispatcher Dispatcher);

    private sealed class Injectable : ICoreIteratorHandlerTypesInjectable<InjectableArg, object>
    {
        object ICoreIteratorHandlerTypesInjectable<InjectableArg, object>.WithInjectedTypes<
            TIterator,
            TItem,
            TIHandler,
            TProxy,
            TIPipeline,
            TPipelineProxy
        >(InjectableArg arg)
        {
            return new TProxy
            {
                ServiceProvider = arg.ServiceProvider,
                Dispatcher = arg.Dispatcher,
                Pipeline = new IteratorPipeline<TIterator, TItem>(handlerType: null, arg.ServiceProvider),
            };
        }
    }
}
