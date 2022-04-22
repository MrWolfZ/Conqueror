using System;
using System.Linq;
using Conqueror.Common;
using Conqueror.Eventing.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventingServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public int ConfigurationPhase => 1;
        
        public void Configure(IServiceCollection services)
        {
            ConfigureEventObservers(services);
            ConfigureEventObserverMiddlewares(services);
            ConfigureEventPublisherMiddlewares(services);
        }

        private static void ConfigureEventObservers(IServiceCollection services)
        {
            var observerTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                        .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                        .OfType<Type>()
                                        .Where(t => t.IsAssignableTo(typeof(IEventObserver)))
                                        .ToList();

            foreach (var observerType in observerTypes)
            {
                RegisterMetadata(observerType);
                RegisterPlainInterfaces(observerType);
                RegisterCustomerInterfaces(observerType);
            }

            void RegisterMetadata(Type observerType)
            {
                foreach (var plainInterfaceType in observerType.GetEventObserverInterfaceTypes())
                {
                    var eventType = plainInterfaceType.GetGenericArguments().First();

                    _ = services.AddSingleton(new EventObserverMetadata(eventType, observerType, new()));
                }
            }

            void RegisterPlainInterfaces(Type observerType)
            {
                foreach (var plainInterfaceType in observerType.GetEventObserverInterfaceTypes())
                {
                    var eventType = plainInterfaceType.GetGenericArguments().First();
                    
                    _ = services.AddTransient(typeof(IEventObserver<>).MakeGenericType(eventType), typeof(EventObserverProxy<>).MakeGenericType(eventType));
                }
            }

            void RegisterCustomerInterfaces(Type observerType)
            {
                foreach (var customInterfaceType in observerType.GetCustomEventObserverInterfaceTypes())
                {
                    foreach (var plainInterfaceType in customInterfaceType.GetEventObserverInterfaceTypes())
                    {
                        var dynamicType = DynamicType.Create(customInterfaceType, plainInterfaceType);
                        _ = services.AddTransient(customInterfaceType, dynamicType);
                    }
                }
            }
        }

        private static void ConfigureEventObserverMiddlewares(IServiceCollection services)
        {
            foreach (var middlewareType in services.Where(d => d.ServiceType == d.ImplementationType).Select(d => d.ImplementationType).OfType<Type>().ToList())
            {
                var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsEventObserverMiddlewareInterface).ToList();

                switch (middlewareInterfaces.Count)
                {
                    case < 1:
                        continue;

                    case > 1:
                        throw new ArgumentException($"type {middlewareType.Name} implements {typeof(IEventObserverMiddleware<>).Name} more than once");
                }
            }

            static bool IsEventObserverMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventObserverMiddleware<>);
        }

        private static void ConfigureEventPublisherMiddlewares(IServiceCollection services)
        {
            foreach (var middlewareType in services.Where(d => d.ServiceType == d.ImplementationType).Select(d => d.ImplementationType).OfType<Type>().ToList())
            {
                var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsEventPublisherMiddlewareInterface).ToList();

                switch (middlewareInterfaces.Count)
                {
                    case < 1:
                        continue;

                    case > 1:
                        throw new ArgumentException($"type {middlewareType.Name} implements {typeof(IEventObserverMiddleware<>).Name} more than once");

                    default:
                        _ = services.AddTransient<IEventPublisherMiddlewareInvoker>(_ => new EventPublisherMiddlewareInvoker(middlewareType));
                        break;
                }
            }

            static bool IsEventPublisherMiddlewareInterface(Type i) => i == typeof(IEventPublisherMiddleware);
        }
    }
}
