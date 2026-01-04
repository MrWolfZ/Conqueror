namespace Conqueror.Iterating;

internal sealed class IteratorHandlerRegistry(
    IServiceProvider serviceProvider,
    IEnumerable<IteratorHandlerRegistration> registrations
) : IIteratorHandlerRegistry
{
    private readonly ConcurrentDictionary<
        (Type IteratorType, Type InjectorType),
        IIteratorServerHandlerInvoker?
    > invokerByIteratorAndInjectorType = [];

    private readonly ConcurrentDictionary<Type, List<IIteratorServerHandlerInvoker>> invokersByInjectorType = [];

    private readonly Dictionary<Type, IteratorHandlerRegistration> registrationByIteratorType =
        registrations.ToDictionary(r => r.IteratorType);

    public IIteratorServerHandlerInvoker<TTypesInjector>? GetServerHandlerInvoker<TIterator, TItem, TTypesInjector>()
        where TIterator : class, IIterator<TIterator, TItem>
        where TTypesInjector : class, IIteratorHandlerTypesInjector
    {
        var key = (typeof(TIterator), typeof(TTypesInjector));

        return invokerByIteratorAndInjectorType.GetOrAdd(
                key,
                static (_, @this) => @this.GetInvokerForIteratorAndInjectorType<TIterator, TTypesInjector>(),
                this
            ) as IIteratorServerHandlerInvoker<TTypesInjector>;
    }

    public IReadOnlyCollection<IIteratorServerHandlerInvoker<TTypesInjector>> GetServerHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, IIteratorHandlerTypesInjector
    {
        return invokersByInjectorType
            .GetOrAdd(
                typeof(TTypesInjector),
                static (_, @this) => [.. @this.PopulateIteratorInvokersForServer<TTypesInjector>()],
                this
            )
            .Cast<IIteratorServerHandlerInvoker<TTypesInjector>>()
            .ToList();
    }

    private List<IIteratorServerHandlerInvoker> PopulateIteratorInvokersForServer<TTypesInjector>()
        where TTypesInjector : class, IIteratorHandlerTypesInjector
    {
        var invokers =
            from r in registrationByIteratorType.Values
            let typesInjector = r
                .TypeInjectors.OfType<TTypesInjector>()
                .FirstOrDefault(i => i.IteratorType == r.IteratorType)
            where typesInjector is not null
            let handlerInvoker = r.HandlerInvokerFactory(serviceProvider)
            select (IIteratorServerHandlerInvoker)
                new IteratorServerHandlerInvoker<TTypesInjector>(r, handlerInvoker, typesInjector);

        return invokers.ToList();
    }

    private IteratorServerHandlerInvoker<TTypesInjector>? GetInvokerForIteratorAndInjectorType<
        TIterator,
        TTypesInjector
    >()
        where TTypesInjector : class, IIteratorHandlerTypesInjector
    {
        var registration = registrationByIteratorType.GetValueOrDefault(typeof(TIterator));

        if (registration is null)
        {
            return null;
        }

        var typesInjector = registration
            .TypeInjectors.OfType<TTypesInjector>()
            .FirstOrDefault(i => i.IteratorType == registration.IteratorType);
        var handlerInvoker = registration.HandlerInvokerFactory(serviceProvider);

        return typesInjector is null
            ? null
            : new IteratorServerHandlerInvoker<TTypesInjector>(registration, handlerInvoker, typesInjector);
    }
}

internal sealed record IteratorHandlerRegistration(
    Type IteratorType,
    Type ItemType,
    Type? HandlerType,
    Delegate? HandlerFn,
    Func<IServiceProvider, IIteratorHandlerInvoker> HandlerInvokerFactory,
    IReadOnlyCollection<IIteratorHandlerTypesInjector> TypeInjectors
);
