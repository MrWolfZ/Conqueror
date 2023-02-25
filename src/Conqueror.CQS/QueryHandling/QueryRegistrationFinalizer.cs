using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryRegistrationFinalizer : IConquerorRegistrationFinalizer
    {
        private readonly IServiceCollection services;

        public QueryRegistrationFinalizer(IServiceCollection services)
        {
            this.services = services;
        }

        public int ExecutionPhase => 1;

        public void Execute()
        {
            ConfigureHandlers(services);
            ConfigureMiddlewares(services);
        }

        private static void ConfigureHandlers(IServiceCollection services)
        {
            var handlerTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                       .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                       .Concat(services.Where(d => !d.ServiceType.IsInterface && !d.ServiceType.IsAbstract && d.ImplementationFactory != null).Select(d => d.ServiceType))
                                       .OfType<Type>()
                                       .Where(t => t.IsAssignableTo(typeof(IQueryHandler)))
                                       .Distinct()
                                       .ToList();

            var registrations = new List<QueryHandlerRegistration>();

            foreach (var handlerType in handlerTypes)
            {
                var pipelineConfigurationAction = GetPipelineConfigurationAction(handlerType);

                services.AddConquerorQueryClient(handlerType, b => new InMemoryQueryTransport(b.ServiceProvider, handlerType), pipelineConfigurationAction);

                foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
                {
                    var registration = new QueryHandlerRegistration(queryType, responseType, handlerType);
                    registrations.Add(registration);
                    services.AddSingleton(registration);
                }
            }

            var duplicateRegistrations = registrations.GroupBy(t => t.QueryType).FirstOrDefault(g => g.Count() > 1);

            if (duplicateRegistrations is not null)
            {
                var queryType = duplicateRegistrations.Key;
                var duplicateHandlerTypes = duplicateRegistrations.Select(h => h.HandlerType);
                throw new InvalidOperationException($"only a single handler for query type {queryType} is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
            }

            Action<IQueryPipelineBuilder>? GetPipelineConfigurationAction(Type handlerType)
            {
                var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is QueryHandlerPipelineConfiguration c && c.HandlerType == handlerType)
                                                    ?.ImplementationInstance as QueryHandlerPipelineConfiguration;

                return existingConfiguration?.Configure ?? CreatePipelineConfigurationFunction(handlerType);
            }

            static Action<IQueryPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
            {
                if (!handlerType.IsAssignableTo(typeof(IConfigureQueryPipeline)))
                {
                    return null;
                }

#if NET7_0_OR_GREATER
                var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureQueryPipeline)).TargetMethods.Single();
#else
                const string configurationMethodName = "ConfigurePipeline";

                var pipelineConfigurationMethod = handlerType.GetMethod(configurationMethodName, BindingFlags.Public | BindingFlags.Static);

                if (pipelineConfigurationMethod is null)
                {
                    throw new InvalidOperationException(
                        $"query handler type '{handlerType.Name}' implements the interface '{nameof(IConfigureQueryPipeline)}' but does not have a public method '{configurationMethodName}'");
                }

                var methodHasInvalidReturnType = pipelineConfigurationMethod.ReturnType != typeof(void);
                var methodHasInvalidParameterTypes = pipelineConfigurationMethod.GetParameters().Length != 1
                                                     || pipelineConfigurationMethod.GetParameters().Single().ParameterType != typeof(IQueryPipelineBuilder);

                if (methodHasInvalidReturnType || methodHasInvalidParameterTypes)
                {
                    throw new InvalidOperationException(
                        $"query handler type '{handlerType.Name}' has an invalid method signature for '{configurationMethodName}'; ensure that the signature is 'public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)'");
                }
#endif

                var builderParam = Expression.Parameter(typeof(IQueryPipelineBuilder));
                var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
                var lambda = Expression.Lambda(body, builderParam).Compile();
                return (Action<IQueryPipelineBuilder>)lambda;
            }
        }

        private static void ConfigureMiddlewares(IServiceCollection services)
        {
            var middlewareTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                          .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                          .Concat(services.Where(d => !d.ServiceType.IsInterface && !d.ServiceType.IsAbstract && d.ImplementationFactory != null).Select(d => d.ServiceType))
                                          .OfType<Type>()
                                          .Where(HasQueryMiddlewareInterface)
                                          .Distinct()
                                          .ToList();

            foreach (var middlewareType in middlewareTypes)
            {
                var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsQueryMiddlewareInterface).ToList();

                switch (middlewareInterfaces.Count)
                {
                    case < 1:
                        continue;

                    case > 1:
                        throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IQueryMiddleware)} more than once");
                }
            }

            var configurationMethod = typeof(QueryRegistrationFinalizer).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

            if (configurationMethod == null)
            {
                throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
            }

            var registrations = new List<QueryMiddlewareRegistration>();

            foreach (var middlewareType in middlewareTypes)
            {
                registrations.Add(new(middlewareType, GetMiddlewareConfigurationType(middlewareType)));

                var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullQueryMiddlewareConfiguration));

                try
                {
                    _ = genericConfigurationMethod.Invoke(null, new object[] { services });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            _ = services.AddSingleton(new QueryMiddlewareRegistry(registrations))
                        .AddSingleton<IQueryMiddlewareRegistry>(p => p.GetRequiredService<QueryMiddlewareRegistry>());

            _ = services.AddSingleton<IReadOnlyDictionary<Type, IQueryMiddlewareInvoker>>(p => p.GetRequiredService<IEnumerable<IQueryMiddlewareInvoker>>().ToDictionary(i => i.MiddlewareType));
        }

        private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
        {
            _ = services.AddSingleton<IQueryMiddlewareInvoker, QueryMiddlewareInvoker<TMiddleware, TConfiguration>>();
        }

        private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsQueryMiddlewareInterface).GetGenericArguments().FirstOrDefault();

        private static bool HasQueryMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsQueryMiddlewareInterface);

        private static bool IsQueryMiddlewareInterface(Type i) => i == typeof(IQueryMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>));
    }
}
