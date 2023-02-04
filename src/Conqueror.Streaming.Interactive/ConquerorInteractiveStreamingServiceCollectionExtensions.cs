using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Common;
using Conqueror.Streaming.Interactive;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorInteractiveStreamingServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorInteractiveStreaming(this IServiceCollection services)
        {
            services.AddFinalizationCheck();

            services.TryAddSingleton<InteractiveStreamingHandlerRegistry>();
            //// TODO
            //// services.TryAddSingleton<InteractiveStreamingMiddlewaresInvoker>();
            services.TryAddSingleton(new InteractiveStreamingRegistrationFinalizer(services));
            //// TODO
            //// services.TryAddSingleton<InteractiveStreamingContextAccessor>();
            //// services.TryAddSingleton<IInteractiveStreamingContextAccessor>(p => p.GetRequiredService<InteractiveStreamingContextAccessor>());

            services.TryAddSingleton<ConquerorContextAccessor>();
            services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<ConquerorContextAccessor>());

            return services;
        }

        public static IServiceCollection AddConquerorInteractiveStreamingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var validTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract).ToList();

            foreach (var handlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IInteractiveStreamingHandler))))
            {
                services.TryAddTransient(handlerType);
            }

            // TODO
            // foreach (var middlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsInteractiveStreamingMiddlewareInterface)))
            // {
            //     services.TryAddTransient(middlewareType);
            // }

            return services;

            // static bool IsInteractiveStreamingMiddlewareInterface(Type i) => i == typeof(IInteractiveStreamingMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInteractiveStreamingMiddleware<>));
        }

        public static IServiceCollection AddConquerorInteractiveStreamingTypesFromExecutingAssembly(this IServiceCollection services)
        {
            return services.AddConquerorInteractiveStreamingTypesFromAssembly(Assembly.GetCallingAssembly());
        }
    }
}
