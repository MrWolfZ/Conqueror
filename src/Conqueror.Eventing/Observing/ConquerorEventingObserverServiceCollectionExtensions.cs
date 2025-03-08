using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.Eventing.Observing;
using Conqueror.Eventing.Publishing;
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
        return services.AddConquerorEventObserverDelegateRegistration(observerFn, null);
    }

    public static IServiceCollection AddConquerorEventObserverDelegate<TEvent>(this IServiceCollection services,
                                                                               Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                               Action<IEventPipeline<TEvent>> configurePipeline)
        where TEvent : class
    {
        return services.AddConquerorEventObserverDelegateRegistration(observerFn, configurePipeline);
    }

    internal static void TryAddConquerorEventObserver(this IServiceCollection services,
                                                      Type observerType,
                                                      ServiceDescriptor serviceDescriptor)
    {
        services.TryAdd(serviceDescriptor);
        services.AddObserverInternal(observerType);
    }

    private static IServiceCollection AddConquerorEventObserver(this IServiceCollection services,
                                                                Type observerType,
                                                                ServiceDescriptor serviceDescriptor)
    {
        services.Replace(serviceDescriptor);
        return services.AddObserverInternal(observerType);
    }

    private static IServiceCollection AddConquerorEventObserverDelegateRegistration<TEvent>(this IServiceCollection services,
                                                                                            Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                                                                            Action<IEventPipeline<TEvent>>? configurePipeline)
        where TEvent : class
    {
        services.AddConquerorEventing();

        services.AddSingleton<IEventObserverInvoker>(new EventObserverInvoker<TEvent>(configurePipeline, observerFn, null));

        // add the dispatcher for the event type eagerly to prevent the minor performance hit of open generic types
        services.TryAddEnumerable(ServiceDescriptor.Transient<IEventObserver<TEvent>, EventObserverDispatcher<TEvent>>(p => new(p, p.GetRequiredService<EventPublisherDispatcher>())));

        return services;
    }

    private static IServiceCollection AddObserverInternal(this IServiceCollection services, Type observerType)
    {
        services.AddConquerorEventing();

        observerType.ValidateNoInvalidEventObserverInterface();

        var method = typeof(ConquerorEventingObserverServiceCollectionExtensions).GetMethod(nameof(AddObserverInvoker), BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddObserverInvoker)}'");
        }

        foreach (var observedEventType in observerType.GetObservedEventTypes())
        {
            var genericMethod = method.MakeGenericMethod(observerType, observedEventType);

            try
            {
                _ = genericMethod.Invoke(null, [services]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddObserverInvoker<TObserver, TEvent>(this IServiceCollection services)
        where TObserver : class, IEventObserver<TEvent>
        where TEvent : class
    {
        var existingInvokerDescriptor = services.FirstOrDefault(d => d.ImplementationInstance is EventObserverInvoker<TEvent> i
                                                                     && i.ObserverType == typeof(TObserver)
                                                                     && i.EventType == typeof(TEvent));

        if (existingInvokerDescriptor is not null)
        {
            return;
        }

        services.AddCustomObserverInterfaces<TObserver, TEvent>();

        var pipelineConfigurationAction = CreatePipelineConfigurationFunction(typeof(TObserver));

        var invoker = new EventObserverInvoker<TEvent>(pipelineConfigurationAction,
                                                       (evt, p, ct) => p.GetRequiredService<TObserver>().Handle(evt, ct),
                                                       typeof(TObserver));

        services.AddSingleton<IEventObserverInvoker>(invoker);

        // add the dispatcher for the event type eagerly to prevent the minor performance hit of open generic types
        services.TryAddEnumerable(ServiceDescriptor.Transient<IEventObserver<TEvent>, EventObserverDispatcher<TEvent>>(p => new(p, p.GetRequiredService<EventPublisherDispatcher>())));

        return;

        static Action<IEventPipeline<TEvent>> CreatePipelineConfigurationFunction(Type observerType)
        {
            var pipelineConfigurationMethod = observerType.GetInterfaceMap(typeof(IEventObserver<TEvent>)).TargetMethods.Single(m => m.Name == nameof(IEventObserver<TEvent>.ConfigurePipeline));

            var builderParam = Expression.Parameter(typeof(IEventPipeline<TEvent>));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IEventPipeline<TEvent>>)lambda;
        }
    }

    private static void AddCustomObserverInterfaces<TObserver, TEvent>(this IServiceCollection services)
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
                    $"event observer type '{typeof(TObserver).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom event observer interface types are not allowed to have any additional methods beside the '{nameof(IEventObserver<object>.Handle)}' inherited from '{typeof(IEventObserver<>).Name}'");
            }

            return customHandlerInterface;
        }
    }
}
