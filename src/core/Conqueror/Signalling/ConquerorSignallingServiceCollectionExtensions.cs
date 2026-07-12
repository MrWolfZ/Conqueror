#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using Conqueror;
using Conqueror.Signalling;

public static class ConquerorSignallingServiceCollectionExtensions
{
    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        THandler
    >(this IServiceCollection services)
        where THandler : class, ISignalHandler, ISignalHandlerWithSourceGeneration
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(
            new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        THandler
    >(this IServiceCollection services, ServiceLifetime lifetime)
        where THandler : class, ISignalHandler, ISignalHandlerWithSourceGeneration
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(
            new(typeof(THandler), typeof(THandler), lifetime),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        THandler
    >(this IServiceCollection services, Func<IServiceProvider, THandler> factory)
        where THandler : class, ISignalHandler, ISignalHandlerWithSourceGeneration
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(
            new(typeof(THandler), factory, ServiceLifetime.Transient),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        THandler
    >(this IServiceCollection services, Func<IServiceProvider, THandler> factory, ServiceLifetime lifetime)
        where THandler : class, ISignalHandler, ISignalHandlerWithSourceGeneration
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(
            new(typeof(THandler), factory, lifetime),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        THandler
    >(this IServiceCollection services, THandler instance)
        where THandler : class, ISignalHandler, ISignalHandlerWithSourceGeneration
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(
            new(typeof(THandler), instance),
            shouldOverwriteRegistration: true
        );
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerFn<TSignal> fn
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler> =>
        services.AddSignalHandlerDelegateInternal(signalTypes, fn, configurePipeline: null, typesInjector: null);

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerSyncFn<TSignal> fn
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return services.AddSignalHandlerDelegateInternal(
            signalTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.CompletedTask;
            },
            configurePipeline: null,
            typesInjector: null
        );
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler> =>
        services.AddSignalHandlerDelegateInternal(signalTypes, fn, configurePipeline, typesInjector: null);

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return services.AddSignalHandlerDelegateInternal(
            signalTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.CompletedTask;
            },
            configurePipeline,
            typesInjector: null
        );
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>>? configurePipeline,
        ISignalHandlerTypesInjector typesInjector
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler> =>
        services.AddSignalHandlerDelegateInternal(signalTypes, fn, configurePipeline, typesInjector);

    public static IServiceCollection AddSignalHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorSignalling();

        SignalHandlerTypeServiceRegistry.RunWithRegisteredTypes(new ServiceRegisterable(services, assembly));

        return services;
    }

    internal static IServiceCollection AddConquerorSignalling(this IServiceCollection services)
    {
        // when creating publishers, we can use a singleton dispatcher since it is not bound to a handler type
        services.TryAddSingleton<ISignalDispatcher>(static p => new SignalDispatcher(
            p.GetRequiredService<IConquerorContextAccessor>(),
            p.GetRequiredService<ISignalIdFactory>(),
            SignalTransportRole.Publisher,
            handlerType: null
        ));

        services.TryAddTransient<ISignalPublishers, SignalPublishers>();
        services.TryAddSingleton<IInProcessSignalPublisherFactory, InProcessSignalPublisherFactory>();
        services.TryAddSingleton<IAggregateSignalPublisherFactory, AggregateSignalPublisherFactory>();
        services.TryAddTransient<ISignalReceivers, SignalReceivers>();
        services.TryAddSingleton<ISignalIdFactory, DefaultSignalIdFactory>();
        services.TryAddSingleton<SignalHandlerRegistry>();
        services.TryAddSingleton<ISignalHandlerRegistry>(static p => p.GetRequiredService<SignalHandlerRegistry>());
        services.TryAddSingleton<InProcessSignalReceiver>();

        return services.AddConquerorContext();
    }

    private static IServiceCollection AddSignalHandlerInternalGeneric<THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    )
        where THandler : class, ISignalHandler
    {
        if (typeof(THandler).IsInterface || typeof(THandler).IsAbstract)
        {
            throw new InvalidOperationException(
                $"handler type '{typeof(THandler)}' must not be an interface or abstract class"
            );
        }

        var typesInjectors = THandler.GetTypeInjectors().ToList();
        foreach (var injector in typesInjectors.OfType<ICoreSignalHandlerTypesInjector>())
        {
            injector.Inject(
                new SignalHandlerRegistrationTypeInjectable(services, serviceDescriptor, shouldOverwriteRegistration),
                new(typeof(THandler), typesInjectors, injector.ConfigurePipeline)
            );
        }

        return services;
    }

    private static IServiceCollection AddSignalHandlerDelegateInternal<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> _, // for type inference
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>>? configurePipeline,
        ISignalHandlerTypesInjector? typesInjector
    )
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        services.AddConquerorSignalling();

        var typesInjectors = typesInjector is null
            ? new[] { TSignal.CoreTypesInjector }
            : [TSignal.CoreTypesInjector, typesInjector];
        services.AddSingleton(
            new SignalHandlerRegistration(
                typeof(TSignal),
                HandlerType: null,
                fn,
                p => new SignalHandlerInvoker<TSignal>(
                    p.GetRequiredService<IConquerorContextAccessor>(),
                    p.GetRequiredService<ISignalIdFactory>(),
                    configurePipeline,
                    fn,
                    handlerType: null
                ),
                typesInjectors
            )
        );

        return services;
    }

    private sealed class ServiceRegisterable(IServiceCollection services, Assembly assembly)
        : ISignalHandlerServiceRegisterable
    {
        public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
            where THandler : class, ISignalHandler
        {
            if (
                typeof(THandler).Assembly == assembly
                && typeof(THandler)
                    is
                    {
                        IsInterface: false,
                        IsAbstract: false,
                        ContainsGenericParameters: false,
                        IsNestedPrivate: false,
                        IsNestedFamily: false,
                    }
            )
            {
                _ = services.AddSignalHandlerInternalGeneric<THandler>(
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
    private readonly record struct SignalHandlerRegistrationTypeInjectableArg(
        Type HandlerType,
        List<ISignalHandlerTypesInjector> TypeInjectors,
        Delegate? ConfigurePipeline
    );

    private sealed class SignalHandlerRegistrationTypeInjectable(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    ) : ICoreSignalHandlerTypesInjectable<SignalHandlerRegistrationTypeInjectableArg, IServiceCollection>
    {
        IServiceCollection ICoreSignalHandlerTypesInjectable<
            SignalHandlerRegistrationTypeInjectableArg,
            IServiceCollection
        >.WithInjectedTypes<TSignal, TIHandler, TProxy>(SignalHandlerRegistrationTypeInjectableArg arg)
        {
            var existingRegistration = services.SingleOrDefault(d =>
                d.ImplementationInstance is SignalHandlerRegistration r
                && r.SignalType == typeof(TSignal)
                && r.HandlerType == arg.HandlerType
            );

            var configurePipeline = arg.ConfigurePipeline as Action<ISignalPipeline<TSignal>>;

            Debug.Assert(
                configurePipeline is not null,
                "the handler registration injectable should only be called from the types injector of a concrete handler type"
            );

            var registration = new SignalHandlerRegistration(
                typeof(TSignal),
                arg.HandlerType,
                HandlerFn: null,
                p => new SignalHandlerInvoker<TSignal>(
                    p.GetRequiredService<IConquerorContextAccessor>(),
                    p.GetRequiredService<ISignalIdFactory>(),
                    configurePipeline,
                    (n, provider, ct) =>
                        TSignal.InvokeHandler((TIHandler)provider.GetRequiredService(arg.HandlerType), n, ct),
                    arg.HandlerType
                ),
                arg.TypeInjectors
            );

            if (existingRegistration is not null)
            {
                if (shouldOverwriteRegistration)
                {
                    services.Remove(existingRegistration);
                    services.AddSingleton(registration);
                }
            }
            else
            {
                services.AddSingleton(registration);
            }

            services.AddConquerorSignalling();

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
