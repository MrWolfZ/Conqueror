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
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingObserverServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services,
                                                                          ServiceLifetime lifetime = ServiceLifetime.Transient,
                                                                          Action<IEventObserverTransportBuilder>? configureTransports = null)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(typeof(TObserver), new(typeof(TObserver), typeof(TObserver), lifetime), configureTransports);
    }

    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services,
                                                                          Func<IServiceProvider, TObserver> factory,
                                                                          ServiceLifetime lifetime = ServiceLifetime.Transient,
                                                                          Action<IEventObserverTransportBuilder>? configureTransports = null)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(typeof(TObserver), new(typeof(TObserver), factory, lifetime), configureTransports);
    }

    public static IServiceCollection AddConquerorEventObserver<TObserver>(this IServiceCollection services,
                                                                          TObserver instance,
                                                                          Action<IEventObserverTransportBuilder>? configureTransports = null)
        where TObserver : class, IEventObserver
    {
        return services.AddConquerorEventObserver(typeof(TObserver), new(typeof(TObserver), instance), configureTransports);
    }

    public static IServiceCollection AddConquerorEventObserverDelegate<TEvent>(this IServiceCollection services,
                                                                               Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                               Action<IEventObserverTransportBuilder>? configureTransports = null)
        where TEvent : class
    {
        services.AddConquerorEventing();

        return services.AddConquerorEventObserverDelegateRegistration(observerFn, null, configureTransports);
    }

    public static IServiceCollection AddConquerorEventObserverDelegate<TEvent>(this IServiceCollection services,
                                                                               Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                               Action<IEventObserverPipelineBuilder> configurePipeline,
                                                                               Action<IEventObserverTransportBuilder>? configureTransports = null)
        where TEvent : class
    {
        services.AddConquerorEventing();

        return services.AddConquerorEventObserverDelegateRegistration(observerFn, configurePipeline, configureTransports);
    }

    internal static IServiceCollection AddConquerorEventObserver(this IServiceCollection services,
                                                                 Type observerType,
                                                                 ServiceDescriptor serviceDescriptor,
                                                                 Action<IEventObserverTransportBuilder>? configureTransports)
    {
        observerType.ValidateNoInvalidEventObserverInterface();

        services.AddConquerorEventing();

        services.Replace(serviceDescriptor);

        return services.AddConquerorEventObserverRegistration(observerType, CreatePipelineConfigurationFunction(observerType), configureTransports)
                       .AddCustomObserverInterfaces(observerType);
    }

    internal static bool IsEventObserverRegistered(this IServiceCollection services, Type observerType)
    {
        return services.Any(d => d.ImplementationInstance is EventObserverRegistration r && r.ObserverType == observerType);
    }

    private static IServiceCollection AddConquerorEventObserverRegistration(this IServiceCollection services,
                                                                            Type observerType,
                                                                            Action<IEventObserverPipelineBuilder>? configurePipeline,
                                                                            Action<IEventObserverTransportBuilder>? configureTransports)
    {
        var existingObserverRegistration = services.FirstOrDefault(d => d.ImplementationInstance is EventObserverRegistration r && r.ObserverType == observerType);

        if (existingObserverRegistration is not null)
        {
            services.Remove(existingObserverRegistration);
        }

        var observerId = new ConquerorEventObserverId(Guid.NewGuid());

        var registration = new EventObserverRegistration(observerId, observerType.GetObservedEventTypes(), observerType, null, configurePipeline, configureTransports);
        services.AddSingleton(registration);

        return services;
    }

    private static IServiceCollection AddConquerorEventObserverDelegateRegistration<TEvent>(this IServiceCollection services,
                                                                                            Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                                            Action<IEventObserverPipelineBuilder>? configurePipeline,
                                                                                            Action<IEventObserverTransportBuilder>? configureTransports)
        where TEvent : class
    {
        Task UntypedObserverFn(object evt, IServiceProvider provider, CancellationToken cancellationToken) =>
            observerFn((TEvent)evt, provider, cancellationToken);

        var observerId = new ConquerorEventObserverId(Guid.NewGuid());

        var registration = new EventObserverRegistration(observerId, [typeof(TEvent)], null, UntypedObserverFn, configurePipeline, configureTransports);
        services.AddSingleton(registration);

        return services;
    }

    private static IServiceCollection AddCustomObserverInterfaces(this IServiceCollection services, Type observerType)
    {
        var openGenericMethod = typeof(ConquerorEventingObserverServiceCollectionExtensions).GetMethod(nameof(AddCustomObserverInterfacesGeneric), BindingFlags.NonPublic | BindingFlags.Static);

        if (openGenericMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddCustomObserverInterfacesGeneric)}'");
        }

        foreach (var eventType in observerType.GetObservedEventTypes())
        {
            var method = openGenericMethod.MakeGenericMethod(observerType, eventType);

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
                throw new InvalidOperationException($"event observer type '{typeof(TObserver).Name}' implements more than one custom interface for query '{typeof(TEvent).Name}'");
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

    private static Action<IEventObserverPipelineBuilder>? CreatePipelineConfigurationFunction(Type observerType)
    {
        if (!observerType.IsAssignableTo(typeof(IConfigureEventObserverPipeline)))
        {
            return null;
        }

        var pipelineConfigurationMethod = observerType.GetInterfaceMap(typeof(IConfigureEventObserverPipeline)).TargetMethods.Single();

        var builderParam = Expression.Parameter(typeof(IEventObserverPipelineBuilder));
        var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
        var lambda = Expression.Lambda(body, builderParam).Compile();
        return (Action<IEventObserverPipelineBuilder>)lambda;
    }
}
