using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.Eventing;
using Conqueror.Eventing.Observing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingObserverServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver<TObserver>(ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services, ServiceLifetime lifetime)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(typeof(TObserver), new(typeof(TObserver), typeof(TObserver), lifetime));
    }

    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services,
                                                                          Func<IServiceProvider, TObserver> factory)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(factory, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services,
                                                                          Func<IServiceProvider, TObserver> factory,
                                                                          ServiceLifetime lifetime)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(typeof(TObserver), new(typeof(TObserver), factory, lifetime));
    }

    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services, TObserver instance)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(typeof(TObserver), new(typeof(TObserver), instance));
    }

    public static IServiceCollection AddConquerorEventObserverDelegate<TEvent>(this IServiceCollection services,
                                                                               Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn)
        where TEvent : class
    {
        services.AddConquerorEventing();

        return services.AddConquerorEventObserverDelegateRegistration(observerFn, null);
    }

    public static IServiceCollection AddConquerorEventObserverDelegate<TEvent>(this IServiceCollection services,
                                                                               Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                               Action<IEventObserverPipelineBuilder> configurePipeline)
        where TEvent : class
    {
        services.AddConquerorEventing();

        return services.AddConquerorEventObserverDelegateRegistration(observerFn, configurePipeline);
    }

    internal static IServiceCollection AddConquerorEventObserver(this IServiceCollection services,
                                                                 Type observerType,
                                                                 ServiceDescriptor serviceDescriptor)
    {
        services.AddConquerorEventing();

        services.Replace(serviceDescriptor);

        return services.AddObserverInternal(observerType, null)
                       .AddCustomObserverInterfaces(observerType);
    }

    internal static bool IsEventObserverRegistered(this IServiceCollection services, Type observerType)
    {
        return services.Any(d => d.ImplementationInstance is EventObserverRegistration r && r.ObserverType == observerType);
    }

    private static IServiceCollection AddConquerorEventObserverDelegateRegistration<TEvent>(this IServiceCollection services,
                                                                                            Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                                            Action<IEventObserverPipelineBuilder>? configurePipeline)
        where TEvent : class
    {
        Task UntypedObserverFn(object evt, IServiceProvider provider, CancellationToken cancellationToken) =>
            observerFn((TEvent)evt, provider, cancellationToken);

        var registration = new EventObserverDelegateRegistration(typeof(TEvent), UntypedObserverFn, configurePipeline);
        services.AddSingleton(registration);

        return services;
    }

    private static IServiceCollection AddCustomObserverInterfaces(this IServiceCollection services, Type observerType)
    {
        var addMethodInfo = typeof(ConquerorEventingObserverServiceCollectionExtensions).GetMethod(nameof(AddCustomObserverInterfacesGeneric), BindingFlags.NonPublic | BindingFlags.Static);

        if (addMethodInfo == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddCustomObserverInterfacesGeneric)}'");
        }

        foreach (var eventType in observerType.GetObservedEventTypes())
        {
            var method = addMethodInfo.MakeGenericMethod(observerType, eventType);

            try
            {
                _ = method.Invoke(null, [services]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddCustomObserverInterfacesGeneric<TObserver, TEvent>(this IServiceCollection services)
        where TObserver : class, IEventObserver
        where TEvent : class
    {
        if (GetCustomEventObserverInterfaceType() is { } customInterfaceType)
        {
            var proxyType = ProxyTypeGenerator.Create(customInterfaceType, typeof(IEventObserver<TEvent>), typeof(EventObserverGeneratedProxyBase<TEvent>));
            services.TryAddTransient(customInterfaceType, proxyType);
        }

        static Type? GetCustomEventObserverInterfaceType()
        {
            var interfaces = typeof(TObserver).GetInterfaces()
                                              .Concat([typeof(TObserver)])
                                              .Where(i => i.IsCustomEventObserverInterfaceType<TEvent>())
                                              .ToList();

            if (interfaces.Count < 1)
            {
                return null;
            }

            if (interfaces.Count > 1)
            {
                throw new InvalidOperationException($"event observer type '{typeof(TObserver).Name}' implements more than one custom interface for event '{typeof(TEvent).Name}'");
            }

            var customHandlerInterface = interfaces.Single();

            if (customHandlerInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException(
                    $"event observer type '{typeof(TObserver).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom event observer interface types are not allowed to have any additional methods beside the '{nameof(IEventObserver<object>.HandleEvent)}' inherited from '{typeof(IEventObserver<>).Name}'");
            }

            return customHandlerInterface;
        }
    }

    private static IServiceCollection AddObserverInternal(this IServiceCollection services,
                                                          Type observerType,
                                                          Action<IEventObserverPipelineBuilder>? configurePipeline)
    {
        observerType.ValidateNoInvalidEventObserverInterface();

        var addObserverMethod = typeof(ConquerorEventingObserverServiceCollectionExtensions).GetMethod(nameof(AddObserver), BindingFlags.NonPublic | BindingFlags.Static);

        if (addObserverMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddObserver)}'");
        }

        foreach (var observedEventType in observerType.GetObservedEventTypes())
        {
            var genericAddMethod = addObserverMethod.MakeGenericMethod(observerType, observedEventType);

            try
            {
                _ = genericAddMethod.Invoke(null, [services, configurePipeline]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static IServiceCollection AddObserver<TObserver, TEvent>(this IServiceCollection services,
                                                                     Action<IEventObserverPipelineBuilder>? configurePipeline)
        where TEvent : class
    {
        var existingObserverRegistration = services.FirstOrDefault(d => d.ImplementationInstance is EventObserverRegistration r
                                                                        && r.ObserverType == typeof(TObserver)
                                                                        && r.EventType == typeof(TEvent));

        if (existingObserverRegistration is not null)
        {
            services.Remove(existingObserverRegistration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(typeof(TObserver));

        var registration = new EventObserverRegistration(typeof(TEvent), typeof(TObserver), pipelineConfigurationAction);
        services.AddSingleton(registration);

        return services;

        static Action<IEventObserverPipelineBuilder>? CreatePipelineConfigurationFunction(Type observerType)
        {
            if (!observerType.IsAssignableTo(typeof(IConfigureEventObserverPipeline)))
            {
                return null;
            }

            var pipelineConfigurationMethod = observerType.GetInterfaceMap(typeof(IConfigureEventObserverPipeline)).TargetMethods.Single();

            // var pipelineConfigurationMethod = observerType.GetInterfaceMap(typeof(IEventObserver<TEvent>)).TargetMethods.Single(m => m.Name == nameof(IEventObserver<TEvent>.ConfigurePipeline));

            var builderParam = Expression.Parameter(typeof(IEventObserverPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IEventObserverPipelineBuilder>)lambda;
        }
    }
}
