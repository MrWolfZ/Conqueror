#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using Conqueror;
using Conqueror.Iterating;

public static class ConquerorIteratingServiceCollectionExtensions
{
    public static IServiceCollection AddIteratorHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >(this IServiceCollection services)
        where THandler : class, IIteratorHandler, IIteratorHandlerWithSourceGeneration
    {
        return services.AddIteratorHandlerInternalGeneric<THandler>(
            new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddIteratorHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >(this IServiceCollection services, ServiceLifetime lifetime)
        where THandler : class, IIteratorHandler, IIteratorHandlerWithSourceGeneration
    {
        return services.AddIteratorHandlerInternalGeneric<THandler>(
            new(typeof(THandler), typeof(THandler), lifetime),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddIteratorHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >(this IServiceCollection services, Func<IServiceProvider, THandler> factory)
        where THandler : class, IIteratorHandler
    {
        return services.AddIteratorHandlerInternalGeneric<THandler>(
            new(typeof(THandler), factory, ServiceLifetime.Transient),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddIteratorHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >(this IServiceCollection services, Func<IServiceProvider, THandler> factory, ServiceLifetime lifetime)
        where THandler : class, IIteratorHandler, IIteratorHandlerWithSourceGeneration
    {
        return services.AddIteratorHandlerInternalGeneric<THandler>(
            new(typeof(THandler), factory, lifetime),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddIteratorHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >(this IServiceCollection services, THandler instance)
        where THandler : class, IIteratorHandler, IIteratorHandlerWithSourceGeneration
    {
        return services.AddIteratorHandlerInternalGeneric<THandler>(
            new(typeof(THandler), instance),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddIteratorHandlerDelegate<TIterator, TItem, TIHandler>(
        this IServiceCollection services,
        IteratorTypes<TIterator, TItem, TIHandler> iteratorTypes,
        IteratorHandlerFn<TIterator, TItem> fn
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler> =>
        services.AddIteratorHandlerDelegateInternal(iteratorTypes, fn, configurePipeline: null, typesInjector: null);

    public static IServiceCollection AddIteratorHandlerDelegate<TIterator, TItem, TIHandler>(
        this IServiceCollection services,
        IteratorTypes<TIterator, TItem, TIHandler> iteratorTypes,
        IteratorHandlerFn<TIterator, TItem> fn,
        Action<IIteratorPipeline<TIterator, TItem>> configurePipeline
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler> =>
        services.AddIteratorHandlerDelegateInternal(iteratorTypes, fn, configurePipeline, typesInjector: null);

    public static IServiceCollection AddIteratorHandlerDelegate<TIterator, TItem, TIHandler>(
        this IServiceCollection services,
        IteratorTypes<TIterator, TItem, TIHandler> iteratorTypes,
        IteratorHandlerFn<TIterator, TItem> fn,
        Action<IIteratorPipeline<TIterator, TItem>>? configurePipeline,
        IIteratorHandlerTypesInjector typesInjector
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler> =>
        services.AddIteratorHandlerDelegateInternal(iteratorTypes, fn, configurePipeline, typesInjector);

    public static IServiceCollection AddIteratorHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly
    )
    {
        services.AddConquerorIterating();

        IteratorHandlerTypeServiceRegistry.RunWithRegisteredTypes(new ServiceRegisterable(services, assembly));

        return services;
    }

    internal static IServiceCollection AddConquerorIterating(this IServiceCollection services)
    {
        // when creating iterators we can use a singleton dispatcher since it is not bound to a handler type
        services.TryAddSingleton<IIteratorDispatcher>(static p => new IteratorDispatcher(
            p.GetRequiredService<IConquerorContextAccessor>(),
            p.GetRequiredService<IIteratorIdFactory>(),
            IteratorTransportRole.Client
        ));

        services.TryAddTransient<IIterators, Iterators>();
        services.TryAddSingleton<IInProcessIteratorClientFactory, InProcessIteratorClientFactory>();
        services.TryAddTransient<IIteratorServers, IteratorServers>();
        services.TryAddSingleton<IIteratorIdFactory, DefaultIteratorIdFactory>();
        services.TryAddSingleton<IteratorHandlerRegistry>();
        services.TryAddSingleton<IIteratorHandlerRegistry>(static p => p.GetRequiredService<IteratorHandlerRegistry>());

        return services.AddConquerorContext();
    }

    private static IServiceCollection AddIteratorHandlerInternalGeneric<THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    )
        where THandler : class, IIteratorHandler
    {
        if (typeof(THandler).IsInterface || typeof(THandler).IsAbstract)
        {
            throw new InvalidOperationException(
                $"handler type '{typeof(THandler)}' must not be an interface or abstract class"
            );
        }

        var typesInjectors = THandler.GetTypeInjectors().ToList();
        foreach (var injector in typesInjectors.OfType<ICoreIteratorHandlerTypesInjector>())
        {
            injector.Inject(
                new IteratorHandlerRegistrationTypeInjectable(services, serviceDescriptor, shouldOverwriteRegistration),
                new(typeof(THandler), typesInjectors, injector.ConfigurePipeline)
            );
        }

        return services;
    }

    private static IServiceCollection AddIteratorHandlerDelegateInternal<TIterator, TItem, TIHandler>(
        this IServiceCollection services,
        IteratorTypes<TIterator, TItem, TIHandler> _, // for type inference
        IteratorHandlerFn<TIterator, TItem> fn,
        Action<IIteratorPipeline<TIterator, TItem>>? configurePipeline,
        IIteratorHandlerTypesInjector? typesInjector
    )
        where TIterator : class, IIterator<TIterator, TItem>
        where TIHandler : class, IIteratorHandler<TIterator, TItem, TIHandler>
    {
        services.AddConquerorIterating();

        var typesInjectors = typesInjector is null
            ? new[] { TIterator.CoreTypesInjector }
            : [TIterator.CoreTypesInjector, typesInjector];
        var registration = new IteratorHandlerRegistration(
            typeof(TIterator),
            typeof(TItem),
            HandlerType: null,
            fn,
            p => new IteratorHandlerInvoker<TIterator, TItem>(
                p,
                p.GetRequiredService<IConquerorContextAccessor>(),
                p.GetRequiredService<IIteratorIdFactory>(),
                configurePipeline,
                fn,
                handlerType: null
            ),
            typesInjectors
        );

        var existingRegistration = services.SingleOrDefault(d =>
            d.ImplementationInstance is IteratorHandlerRegistration r && r.IteratorType == typeof(TIterator)
        );

        if (existingRegistration is not null)
        {
            services.Remove(existingRegistration);
        }

        services.AddSingleton(registration);

        return services;
    }

    private sealed class ServiceRegisterable(IServiceCollection services, Assembly assembly)
        : IIteratorHandlerServiceRegisterable
    {
        public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
            where THandler : class, IIteratorHandler
        {
            if (
                typeof(THandler).Assembly == assembly
                && typeof(THandler)
                    is {
                        IsInterface: false,
                        IsAbstract: false,
                        ContainsGenericParameters: false,
                        IsNestedPrivate: false,
                        IsNestedFamily: false,
                    }
            )
            {
                _ = services.AddIteratorHandlerInternalGeneric<THandler>(
                    ServiceDescriptor.Transient<THandler, THandler>(),
                    shouldOverwriteRegistration: false
                );
            }
        }
    }

    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1201:Elements should appear in the correct order",
        Justification = "order makes sense"
    )]
    private readonly record struct IteratorHandlerRegistrationTypeInjectableArg(
        Type HandlerType,
        List<IIteratorHandlerTypesInjector> TypeInjectors,
        Delegate? ConfigurePipeline
    );

    private sealed class IteratorHandlerRegistrationTypeInjectable(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    ) : ICoreIteratorHandlerTypesInjectable<IteratorHandlerRegistrationTypeInjectableArg, IServiceCollection>
    {
        IServiceCollection ICoreIteratorHandlerTypesInjectable<
            IteratorHandlerRegistrationTypeInjectableArg,
            IServiceCollection
        >.WithInjectedTypes<TIterator, TItem, TIHandler, TProxy, TIPipeline, TPipelineProxy>(
            IteratorHandlerRegistrationTypeInjectableArg arg
        )
        {
            var configurePipeline = arg.ConfigurePipeline as Action<TIPipeline>;

            Debug.Assert(
                configurePipeline is not null,
                "the handler registration injectable should only be called from the types injector of a concrete handler type"
            );

            var registration = new IteratorHandlerRegistration(
                typeof(TIterator),
                typeof(TItem),
                arg.HandlerType,
                HandlerFn: null,
                p => new IteratorHandlerInvoker<TIterator, TItem>(
                    p,
                    p.GetRequiredService<IConquerorContextAccessor>(),
                    p.GetRequiredService<IIteratorIdFactory>(),
                    pipeline => configurePipeline(new TPipelineProxy { Wrapped = pipeline }),
                    (iterator, provider, ct) =>
                        TIterator.InvokeHandler((TIHandler)provider.GetRequiredService(arg.HandlerType), iterator, ct),
                    arg.HandlerType
                ),
                arg.TypeInjectors
            );

            var existingRegistration = services.SingleOrDefault(d =>
                d.ImplementationInstance is IteratorHandlerRegistration r && r.IteratorType == typeof(TIterator)
            );

            if (existingRegistration is not null)
            {
                services.Remove(existingRegistration);
            }

            services.AddSingleton(registration);

            services.AddConquerorIterating();

            if (shouldOverwriteRegistration)
            {
                services.Replace(serviceDescriptor);
            }
            else
            {
                services.TryAdd(serviceDescriptor);
            }

            return services;
        }
    }
}
