using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.CQS.QueryHandling;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsQueryMiddlewareServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorQueryMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                  ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IQueryMiddlewareMarker
        {
            return services.AddConquerorQueryMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
        }

        public static IServiceCollection AddConquerorQueryMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                  Func<IServiceProvider, TMiddleware> factory,
                                                                                  ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TMiddleware : class, IQueryMiddlewareMarker
        {
            return services.AddConquerorQueryMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
        }

        public static IServiceCollection AddConquerorQueryMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                  TMiddleware instance)
            where TMiddleware : class, IQueryMiddlewareMarker
        {
            return services.AddConquerorQueryMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
        }

        public static IServiceCollection AddConquerorQueryMiddleware(this IServiceCollection services,
                                                                     Type middlewareType,
                                                                     ServiceDescriptor serviceDescriptor)
        {
            if (services.Any(d => d.ServiceType == middlewareType))
            {
                return services;
            }

            services.Add(serviceDescriptor);
            return services.AddConquerorQueryMiddleware(middlewareType);
        }

        internal static IServiceCollection AddConquerorQueryMiddleware(this IServiceCollection services,
                                                                       Type middlewareType)
        {
            var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsQueryMiddlewareInterface).ToList();

            switch (middlewareInterfaces.Count)
            {
                case < 1:
                    throw new ArgumentException($"type '{middlewareType.Name}' implements no query middleware interface");

                case > 1:
                    throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IQueryMiddleware)} more than once");
            }

            services.AddConquerorCqsQueryServices();

            var configurationMethod = typeof(ConquerorCqsQueryMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

            if (configurationMethod == null)
            {
                throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
            }

            var registration = new QueryMiddlewareRegistration(middlewareType, GetMiddlewareConfigurationType(middlewareType));
            _ = services.AddSingleton(registration);

            var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullQueryMiddlewareConfiguration));

            try
            {
                _ = genericConfigurationMethod.Invoke(null, new object[] { services });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }

            return services;
        }

        private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
        {
            _ = services.AddSingleton<IQueryMiddlewareInvoker, QueryMiddlewareInvoker<TMiddleware, TConfiguration>>();
        }

        private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsQueryMiddlewareInterface).GetGenericArguments().FirstOrDefault();

        private static bool IsQueryMiddlewareInterface(Type i) => i == typeof(IQueryMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>));
    }
}
