using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Conqueror.Common;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public int ConfigurationPhase => 1;

        public void Configure(IServiceCollection services)
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

            var configurationMethod = typeof(QueryServiceCollectionConfigurator).GetMethod(nameof(ConfigureHandler), BindingFlags.NonPublic | BindingFlags.Static);

            if (configurationMethod == null)
            {
                throw new InvalidOperationException($"could not find handler configuration method '{nameof(ConfigureHandler)}'");
            }

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidQueryHandlerInterface();

                foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
                {
                    var genericConfigurationMethod = configurationMethod.MakeGenericMethod(handlerType, queryType, responseType);

                    try
                    {
                        _ = genericConfigurationMethod.Invoke(null, new object[] { services });
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }
                }
            }

            var duplicateMetadata = services.Select(d => d.ImplementationInstance).OfType<QueryHandlerMetadata>().GroupBy(t => t.QueryType).FirstOrDefault(g => g.Count() > 1);

            if (duplicateMetadata is not null)
            {
                var queryType = duplicateMetadata.Key;
                var duplicateHandlerTypes = duplicateMetadata.Select(h => h.HandlerType);
                throw new InvalidOperationException($"only a single handler for query type {queryType} is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
            }
        }

        private static void ConfigureHandler<THandler, TQuery, TResponse>(IServiceCollection services)
            where TQuery : class
        {
            var metadata = new QueryHandlerMetadata(typeof(TQuery), typeof(TResponse), typeof(THandler));
            _ = services.AddSingleton(metadata);

            var pipelineConfigurationAction = GetPipelineConfigurationAction();

            RegisterPlainInterface();
            RegisterCustomInterface();

            void RegisterPlainInterface()
            {
                _ = services.AddTransient<IQueryHandler<TQuery, TResponse>>(p => new QueryHandlerProxy<TQuery, TResponse>(p, metadata, pipelineConfigurationAction));
            }

            void RegisterCustomInterface()
            {
                if (GetCustomQueryHandlerInterfaceType() is { } customInterfaceType)
                {
                    var dynamicType = DynamicType.Create(customInterfaceType, typeof(IQueryHandler<TQuery, TResponse>));
                    services.TryAddTransient(customInterfaceType, dynamicType);
                }
            }

            Action<IQueryPipelineBuilder>? GetPipelineConfigurationAction()
            {
                var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is QueryHandlerPipelineConfiguration c && c.HandlerType == typeof(THandler))
                                                    ?.ImplementationInstance as QueryHandlerPipelineConfiguration;

                return existingConfiguration?.Configure ?? CreatePipelineConfigurationFunction();
            }

            static Action<IQueryPipelineBuilder>? CreatePipelineConfigurationFunction()
            {
                if (!typeof(THandler).IsAssignableTo(typeof(IConfigureQueryPipeline)))
                {
                    return null;
                }

#if NET7_0_OR_GREATER
                var pipelineConfigurationMethod = typeof(THandler).GetInterfaceMap(typeof(IConfigureQueryPipeline)).TargetMethods.Single();
#else
                const string configurationMethodName = "ConfigurePipeline";

                var pipelineConfigurationMethod = typeof(THandler).GetMethod(configurationMethodName, BindingFlags.Public | BindingFlags.Static);

                if (pipelineConfigurationMethod is null)
                {
                    throw new InvalidOperationException(
                        $"query handler type '{typeof(THandler).Name}' implements the interface '{nameof(IConfigureQueryPipeline)}' but does not have a public method '{configurationMethodName}'");
                }

                var methodHasInvalidReturnType = pipelineConfigurationMethod.ReturnType != typeof(void);
                var methodHasInvalidParameterTypes = pipelineConfigurationMethod.GetParameters().Length != 1
                                                     || pipelineConfigurationMethod.GetParameters().Single().ParameterType != typeof(IQueryPipelineBuilder);

                if (methodHasInvalidReturnType || methodHasInvalidParameterTypes)
                {
                    throw new InvalidOperationException(
                        $"query handler type '{typeof(THandler).Name}' has an invalid method signature for '{configurationMethodName}'; ensure that the signature is 'public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)'");
                }
#endif

                var builderParam = Expression.Parameter(typeof(IQueryPipelineBuilder));
                var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
                var lambda = Expression.Lambda(body, builderParam).Compile();
                return (Action<IQueryPipelineBuilder>)lambda;
            }

            static Type? GetCustomQueryHandlerInterfaceType()
            {
                var interfaces = typeof(THandler).GetInterfaces()
                                                 .Where(i => i.IsCustomQueryHandlerInterfaceType<TQuery, TResponse>())
                                                 .ToList();

                if (interfaces.Count < 1)
                {
                    return null;
                }

                if (interfaces.Count > 1)
                {
                    throw new InvalidOperationException($"query handler type '{typeof(THandler).Name}' implements more than one custom interface for query '{typeof(TQuery).Name}'");
                }

                var customHandlerInterface = interfaces.Single();

                if (customHandlerInterface.AllMethods().Count() > 1)
                {
                    throw new ArgumentException(
                        $"query handler type '{typeof(THandler).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom query handler interface types are not allowed to have any additional methods beside the '{nameof(IQueryHandler<object, object>.ExecuteQuery)}' inherited from '{typeof(IQueryHandler<,>).Name}'");
                }

                return customHandlerInterface;
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

            foreach (var middlewareType in middlewareTypes)
            {
                RegisterMetadata(middlewareType);
            }

            void RegisterMetadata(Type middlewareType)
            {
                var configurationType = middlewareType.GetInterfaces().First(IsQueryMiddlewareInterface).GetGenericArguments().FirstOrDefault();

                _ = services.AddSingleton(new QueryMiddlewareMetadata(middlewareType, configurationType));
            }

            static bool HasQueryMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsQueryMiddlewareInterface);

            static bool IsQueryMiddlewareInterface(Type i) => i == typeof(IQueryMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>));
        }
    }
}
