using System;
using System.Linq;
using System.Linq.Expressions;
#if !NET7_0_OR_GREATER
using System.Reflection;
#endif
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandServiceCollectionConfigurator : IServiceCollectionConfigurator
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
                                       .Where(t => t.IsAssignableTo(typeof(ICommandHandler)))
                                       .Distinct()
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidCommandHandlerInterface();
                RegisterMetadata(handlerType);
                RegisterPlainInterfaces(handlerType);
                RegisterCustomInterfaces(handlerType);
                RegisterPipelineConfiguration(handlerType);
            }

            ValidateNoDuplicateCommandTypes();

            void ValidateNoDuplicateCommandTypes()
            {
                var duplicateMetadata = services.Select(d => d.ImplementationInstance).OfType<CommandHandlerMetadata>().GroupBy(t => t.CommandType).FirstOrDefault(g => g.Count() > 1);

                if (duplicateMetadata is not null)
                {
                    var commandType = duplicateMetadata.Key;
                    var duplicateHandlerTypes = duplicateMetadata.Select(h => h.HandlerType);
                    throw new InvalidOperationException($"only a single handler for command type {commandType} is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
                }
            }

            void RegisterMetadata(Type handlerType)
            {
                foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
                {
                    _ = services.AddSingleton(new CommandHandlerMetadata(commandType, responseType, handlerType));
                }
            }

            void RegisterPlainInterfaces(Type handlerType)
            {
                foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
                {
                    if (responseType is null)
                    {
                        _ = services.AddTransient(typeof(ICommandHandler<>).MakeGenericType(commandType), typeof(CommandHandlerProxy<>).MakeGenericType(commandType));
                    }
                    else
                    {
                        _ = services.AddTransient(typeof(ICommandHandler<,>).MakeGenericType(commandType, responseType), typeof(CommandHandlerProxy<,>).MakeGenericType(commandType, responseType));
                    }
                }
            }

            void RegisterCustomInterfaces(Type handlerType)
            {
                foreach (var customInterfaceType in handlerType.GetCustomCommandHandlerInterfaceTypes())
                {
                    foreach (var plainInterfaceType in customInterfaceType.GetCommandHandlerInterfaceTypes())
                    {
                        var dynamicType = DynamicType.Create(customInterfaceType, plainInterfaceType);
                        _ = services.AddTransient(customInterfaceType, dynamicType);
                    }
                }
            }

            void RegisterPipelineConfiguration(Type handlerType)
            {
                var configure = CreatePipelineConfigurationFunction(handlerType);

                if (configure is null)
                {
                    return;
                }

                _ = services.ConfigureCommandPipeline(handlerType, configure);
            }

            static Action<ICommandPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
            {
                if (!handlerType.IsAssignableTo(typeof(IConfigureCommandPipeline)))
                {
                    return null;
                }

#if NET7_0_OR_GREATER
                var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureCommandPipeline)).TargetMethods.Single();
#else
                const string configurationMethodName = "ConfigurePipeline";

                var pipelineConfigurationMethod = handlerType.GetMethod(configurationMethodName, BindingFlags.Public | BindingFlags.Static);

                if (pipelineConfigurationMethod is null)
                {
                    throw new InvalidOperationException(
                        $"command handler type '{handlerType.Name}' implements the interface '{nameof(IConfigureCommandPipeline)}' but does not have a public method '{configurationMethodName}'");
                }

                var methodHasInvalidReturnType = pipelineConfigurationMethod.ReturnType != typeof(void);
                var methodHasInvalidParameterTypes = pipelineConfigurationMethod.GetParameters().Length != 1
                                                     || pipelineConfigurationMethod.GetParameters().Single().ParameterType != typeof(ICommandPipelineBuilder);

                if (methodHasInvalidReturnType || methodHasInvalidParameterTypes)
                {
                    throw new InvalidOperationException(
                        $"command handler type '{handlerType.Name}' has an invalid method signature for '{configurationMethodName}'; ensure that the signature is 'public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)'");
                }
#endif

                var builderParam = Expression.Parameter(typeof(ICommandPipelineBuilder));
                var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
                var lambda = Expression.Lambda(body, builderParam).Compile();
                return (Action<ICommandPipelineBuilder>)lambda;
            }
        }

        private static void ConfigureMiddlewares(IServiceCollection services)
        {
            var middlewareTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                          .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                          .Concat(services.Where(d => !d.ServiceType.IsInterface && !d.ServiceType.IsAbstract && d.ImplementationFactory != null).Select(d => d.ServiceType))
                                          .OfType<Type>()
                                          .Where(HasCommandMiddlewareInterface)
                                          .Distinct()
                                          .ToList();

            foreach (var middlewareType in middlewareTypes)
            {
                var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsCommandMiddlewareInterface).ToList();

                switch (middlewareInterfaces.Count)
                {
                    case < 1:
                        continue;

                    case > 1:
                        throw new ArgumentException($"type {middlewareType.Name} implements {nameof(ICommandMiddleware)} more than once");
                }
            }

            foreach (var middlewareType in middlewareTypes)
            {
                RegisterMetadata(middlewareType);
            }

            void RegisterMetadata(Type middlewareType)
            {
                var configurationType = middlewareType.GetInterfaces().First(IsCommandMiddlewareInterface).GetGenericArguments().FirstOrDefault();

                _ = services.AddSingleton(new CommandMiddlewareMetadata(middlewareType, configurationType));
            }

            static bool HasCommandMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsCommandMiddlewareInterface);

            static bool IsCommandMiddlewareInterface(Type i) => i == typeof(ICommandMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>));
        }
    }
}
