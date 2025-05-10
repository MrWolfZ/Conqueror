using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Signalling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorSignallingServiceCollectionExtensions
{
    public static IServiceCollection AddSignalHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services)
        where THandler : class, ISignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, ISignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, ISignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, ISignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, ISignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), instance), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerFn<TSignal> fn)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return services.AddSignalHandlerDelegateInternal(signalTypes, fn, null);
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerSyncFn<TSignal> fn)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return services.AddSignalHandlerDelegateInternal(signalTypes, (m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, null);
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return services.AddSignalHandlerDelegateInternal(signalTypes, fn, configurePipeline);
    }

    public static IServiceCollection AddSignalHandlerDelegate<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> signalTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        return services.AddSignalHandlerDelegateInternal(signalTypes, (m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, configurePipeline);
    }

    public static IServiceCollection AddSignalHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorSignalling();

        SignalHandlerTypeServiceRegistry.RunWithRegisteredTypes(new ServiceRegisterable(services, assembly));

        return services;
    }

    internal static IServiceCollection AddConquerorSignalling(this IServiceCollection services)
    {
        services.TryAddTransient<ISignalPublishers, SignalPublishers>();
        services.TryAddTransient<ISignalReceivers, SignalReceivers>();
        services.TryAddSingleton<ISignalIdFactory, DefaultSignalIdFactory>();
        services.TryAddSingleton<SignalHandlerRegistry>();
        services.TryAddSingleton<ISignalHandlerRegistry>(p => p.GetRequiredService<SignalHandlerRegistry>());
        services.TryAddSingleton<InProcessSignalReceiver>();

        return services.AddConquerorContext().AddConquerorSingletons();
    }

    private static IServiceCollection AddSignalHandlerInternalGeneric<THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where THandler : class, ISignalHandler
    {
        if (typeof(THandler).IsInterface || typeof(THandler).IsAbstract)
        {
            throw new InvalidOperationException($"handler type '{typeof(THandler)}' must not be an interface or abstract class");
        }

        foreach (var injector in THandler.GetTypeInjectors().OfType<ICoreSignalHandlerTypesInjector>())
        {
            injector.Create(new SignalHandlerRegistrationTypeInjectable(services,
                                                                        serviceDescriptor,
                                                                        shouldOverwriteRegistration));
        }

        return services;
    }

    private static IServiceCollection AddSignalHandlerDelegateInternal<TSignal, TIHandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, TIHandler> _, // for type inference
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>>? configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>
    {
        services.AddConquerorSignalling();

        var invoker = new SignalHandlerInvoker<TSignal>(configurePipeline, fn, null);

        // we do not support configuring the receiver for delegate handlers (yet), since they are mostly
        // designed to support simple testing scenarios, not be used as full-fledged signal handlers
        services.AddSingleton(new SignalHandlerRegistration(typeof(TSignal), null, fn, invoker, [TSignal.CoreTypesInjector]));

        return services;
    }

    private sealed class ServiceRegisterable(IServiceCollection services, Assembly assembly) : ISignalHandlerServiceRegisterable
    {
        public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
            where THandler : class, ISignalHandler
        {
            if (typeof(THandler) is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false, IsNestedFamily: false }
                && (typeof(THandler).DeclaringType?.IsPublic ?? true)
                && typeof(THandler).Assembly == assembly)
            {
                _ = services.AddSignalHandlerInternalGeneric<THandler>(ServiceDescriptor.Transient<THandler, THandler>(), shouldOverwriteRegistration: false);
            }
        }
    }

    private sealed class SignalHandlerRegistrationTypeInjectable(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        : ICoreSignalHandlerTypesInjectable<IServiceCollection>
    {
        IServiceCollection ICoreSignalHandlerTypesInjectable<IServiceCollection>.WithInjectedTypes<TSignal, TIHandler, TProxy, THandler>()
        {
            var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                     && r.SignalType == typeof(TSignal)
                                                                     && r.HandlerType == typeof(THandler));

            var invoker = new SignalHandlerInvoker<TSignal>(
                THandler.ConfigurePipeline,
                (n, p, ct) => TIHandler.Invoke((TIHandler)p.GetRequiredService(typeof(THandler)), n, ct),
                typeof(THandler));

            var registration = new SignalHandlerRegistration(typeof(TSignal), typeof(THandler), null, invoker, THandler.GetTypeInjectors().ToList());

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
