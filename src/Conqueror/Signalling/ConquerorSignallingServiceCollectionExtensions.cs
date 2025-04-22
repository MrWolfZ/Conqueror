using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Signalling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorSignallingServiceCollectionExtensions
{
    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services)
        where THandler : class, IGeneratedSignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedSignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IGeneratedSignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedSignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, IGeneratedSignalHandler
    {
        return services.AddSignalHandlerInternalGeneric<THandler>(new(typeof(THandler), instance), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddSignalHandlerDelegate<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, THandler> signalTypes,
        SignalHandlerFn<TSignal> fn)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        return services.AddSignalHandlerDelegateInternal(fn, null);
    }

    public static IServiceCollection AddSignalHandlerDelegate<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, THandler> signalTypes,
        SignalHandlerSyncFn<TSignal> fn)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        return services.AddSignalHandlerDelegateInternal<TSignal>((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, null);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddSignalHandlerDelegate<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, THandler> signalTypes,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        return services.AddSignalHandlerDelegateInternal(fn, configurePipeline);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddSignalHandlerDelegate<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(
        this IServiceCollection services,
        SignalTypes<TSignal, THandler> signalTypes,
        SignalHandlerSyncFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>> configurePipeline)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>
    {
        return services.AddSignalHandlerDelegateInternal((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, configurePipeline);
    }

    [RequiresUnreferencedCode("Types might be removed")]
    [RequiresDynamicCode("Types might be removed")]
    public static IServiceCollection AddSignalHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorSignalling();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t.IsAssignableTo(typeof(IGeneratedSignalHandler)))
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false, IsNestedFamily: false })
                                 .Where(t => t.DeclaringType?.IsPublic ?? true)
                                 .ToList();

        foreach (var signalHandlerType in validTypes)
        {
            var addHandlerMethod = typeof(ConquerorSignallingServiceCollectionExtensions)
                                   .GetMethod(nameof(AddSignalHandlerInternalGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                                   ?.MakeGenericMethod(signalHandlerType);

            if (addHandlerMethod is null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(ConquerorMessagingServiceCollectionExtensions)}.{nameof(AddSignalHandlerInternalGeneric)}'");
            }

            try
            {
                addHandlerMethod.Invoke(null, [services, ServiceDescriptor.Transient(signalHandlerType, signalHandlerType), false]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    [RequiresUnreferencedCode("Types might be removed")]
    [RequiresDynamicCode("Types might be removed")]
    public static IServiceCollection AddSignalHandlersFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddSignalHandlersFromAssembly(Assembly.GetCallingAssembly());
    }

    internal static IServiceCollection AddConquerorSignalling(this IServiceCollection services)
    {
        services.TryAddTransient<ISignalPublishers, SignalPublishers>();
        services.TryAddSingleton<ISignalIdFactory, DefaultSignalIdFactory>();
        services.TryAddSingleton<SignalTransportRegistry>();
        services.TryAddSingleton<ISignalTransportRegistry>(p => p.GetRequiredService<SignalTransportRegistry>());
        services.TryAddSingleton<InProcessSignalReceiver>();

        return services.AddConquerorContext();
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "we know that both the handler and signal type are reference types and therefore can use shared code")]
    [UnconditionalSuppressMessage("Trimming", "IL2060:Call to \'System.Reflection.MethodInfo.MakeGenericMethod\' can not be statically analyzed. It\'s not possible to guarantee the availability of requirements of the generic method.",
                                  Justification = "we know that both the handler and signal type are reference types and therefore can use shared code")]
    private static IServiceCollection AddSignalHandlerInternalGeneric<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where THandler : class, IGeneratedSignalHandler
    {
        var signalHandlerInterfaces = typeof(THandler).GetInterfaces()
                                                      .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISignalHandler<,>))
                                                      .ToList();

        foreach (var signalHandlerInterface in signalHandlerInterfaces)
        {
            var signalType = signalHandlerInterface.GetGenericArguments()[0];
            var addHandlerMethod = typeof(ConquerorSignallingServiceCollectionExtensions)
                                   .GetMethod(nameof(AddSignalHandlerInternalForSignalType), BindingFlags.NonPublic | BindingFlags.Static)
                                   ?.MakeGenericMethod(signalType, typeof(THandler));

            if (addHandlerMethod is null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(ConquerorMessagingServiceCollectionExtensions)}.{nameof(AddSignalHandlerInternalForSignalType)}'");
            }

            try
            {
                addHandlerMethod.Invoke(null, [services, serviceDescriptor, shouldOverwriteRegistration]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static IServiceCollection AddSignalHandlerInternalForSignalType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, IGeneratedSignalHandler
    {
        return ((IDefaultSignalTypesInjector)TSignal.DefaultTypeInjector.WithHandlerType<THandler>())
            .CreateWithSignalTypes(new SignalHandlerRegistrationTypeInjectable(services,
                                                                               serviceDescriptor,
                                                                               shouldOverwriteRegistration));
    }

    private static IServiceCollection AddSignalHandlerDelegateInternal<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal>(
        this IServiceCollection services,
        SignalHandlerFn<TSignal> fn,
        Action<ISignalPipeline<TSignal>>? configurePipeline)
        where TSignal : class, ISignal<TSignal>
    {
        services.AddConquerorSignalling();

        var invoker = new SignalHandlerInvoker<TSignal>(configurePipeline, fn);

        // we do not support configuring the receiver for delegate handlers (yet), since they are mostly
        // designed to support simple testing scenarios, not be used as full-fledged signal handlers
        services.AddSingleton(new SignalHandlerRegistration(typeof(TSignal), null, fn, invoker, TSignal.TypeInjectors));

        return services;
    }

    private sealed class SignalHandlerRegistrationTypeInjectable(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        : IDefaultSignalTypesInjectable<IServiceCollection>
    {
        IServiceCollection IDefaultSignalTypesInjectable<IServiceCollection>
            .WithInjectedTypes<
                [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                TSignal,
                TGeneratedHandlerInterface,
                TGeneratedHandlerAdapter,
                THandler>()
        {
            if (typeof(THandler).IsInterface || typeof(THandler).IsAbstract)
            {
                throw new InvalidOperationException($"handler type '{typeof(THandler)}' must not be an interface or abstract class");
            }

            var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is SignalHandlerRegistration r
                                                                     && r.SignalType == typeof(TSignal)
                                                                     && r.HandlerType == typeof(THandler));

            var invoker = new SignalHandlerInvoker<TSignal>(
                THandler.ConfigurePipeline,
                (n, p, ct) => TGeneratedHandlerInterface.Invoke((TGeneratedHandlerInterface)p.GetRequiredService(typeof(THandler)), n, ct));

            var typeInjectorsWithHandlerType = TSignal.TypeInjectors.Select(i => i.WithHandlerType<THandler>()).ToList();
            var registration = new SignalHandlerRegistration(typeof(TSignal), typeof(THandler), null, invoker, typeInjectorsWithHandlerType);

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
