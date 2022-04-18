using System;
using System.Linq;
using Conqueror.Eventing.Util;
using Conqueror.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventingServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public void Configure(IServiceCollection services)
        {
            ConfigureEventObservers(services);
            ConfigureEventObserverMiddlewares(services);
            ConfigureEventPublisherMiddlewares(services);
        }

        private static void ConfigureEventObservers(IServiceCollection services)
        {
            var observerTypes = services.Where(d => d.ServiceType == d.ImplementationType)
                                        .Select(d => d.ImplementationType)
                                        .OfType<Type>()
                                        .Where(t => t.IsAssignableTo(typeof(IEventObserver)))
                                        .ToList();

            foreach (var observerType in observerTypes)
            {
                var customInterfaceTypes = observerType.GetCustomEventObserverInterfaceTypes();
                var plainInterfaceTypes = observerType.GetEventObserverInterfaceTypes();

                foreach (var type in plainInterfaceTypes)
                {
                    var eventType = type.GetGenericArguments().First();

                    _ = services.AddSingleton(new EventObserverMetadata(eventType, observerType, new()));
                }

                foreach (var customInterfaceType in customInterfaceTypes)
                {
                    foreach (var i in customInterfaceType.GetEventObserverInterfaceTypes())
                    {
                        var dynamicType = DynamicType.Create(customInterfaceType, i);
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
