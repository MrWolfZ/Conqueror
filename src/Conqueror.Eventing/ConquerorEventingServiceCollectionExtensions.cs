using System;
using System.Linq;
using System.Reflection;
using Conqueror.Eventing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorEventingServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorEventing(this IServiceCollection services)
        {
            services.TryAddTransient<IEventPublisher, EventPublisher>();
            services.TryAddTransient(typeof(IEventObserver<>), typeof(EventObserverProxy<>));
            services.TryAddSingleton<EventObserverRegistry>();
            services.TryAddSingleton<EventObserverMiddlewareRegistry>();
            services.TryAddSingleton<EventMiddlewaresInvoker>();
            services.TryAddSingleton(new EventingServiceCollectionConfigurator());

            return services;
        }

        public static IServiceCollection AddConquerorEventingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var validTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract).ToList();

            foreach (var eventObserverType in validTypes.Where(t => t.IsAssignableTo(typeof(IEventObserver))))
            {
                services.TryAddTransient(eventObserverType);
            }

            foreach (var eventObserverMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsEventObserverMiddlewareInterface)))
            {
                services.TryAddTransient(eventObserverMiddlewareType);
            }

            foreach (var eventPublisherMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(i => i == typeof(IEventPublisherMiddleware))))
            {
                services.TryAddTransient(eventPublisherMiddlewareType);
            }

            return services;

            static bool IsEventObserverMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventObserverMiddleware<>);
        }

        public static IServiceCollection AddConquerorEventingTypesFromExecutingAssembly(this IServiceCollection services)
        {
            return services.AddConquerorEventingTypesFromAssembly(Assembly.GetCallingAssembly());
        }
    }
}
