using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Conqueror.Common;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            var configurationMethod = typeof(CommandServiceCollectionConfigurator).GetMethod(nameof(ConfigureHandler), BindingFlags.NonPublic | BindingFlags.Static);

            if (configurationMethod == null)
            {
                throw new InvalidOperationException($"could not find handler configuration method '{nameof(ConfigureHandler)}'");
            }

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidCommandHandlerInterface();

                foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
                {
                    var genericConfigurationMethod = configurationMethod.MakeGenericMethod(handlerType, commandType, responseType ?? typeof(UnitCommandResponse));

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

            var duplicateMetadata = services.Select(d => d.ImplementationInstance).OfType<CommandHandlerMetadata>().GroupBy(t => t.CommandType).FirstOrDefault(g => g.Count() > 1);

            if (duplicateMetadata is not null)
            {
                var commandType = duplicateMetadata.Key;
                var duplicateHandlerTypes = duplicateMetadata.Select(h => h.HandlerType);
                throw new InvalidOperationException($"only a single handler for command type {commandType} is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
            }
        }

        private static void ConfigureHandler<THandler, TCommand, TResponse>(IServiceCollection services)
            where TCommand : class
        {
            var metadata = new CommandHandlerMetadata(typeof(TCommand), typeof(TResponse), typeof(THandler));
            _ = services.AddSingleton(metadata);

            var pipelineConfigurationAction = GetPipelineConfigurationAction();

            RegisterPlainInterface();
            RegisterCustomInterface();

            void RegisterPlainInterface()
            {
                if (typeof(TResponse) == typeof(UnitCommandResponse))
                {
                    _ = services.AddTransient<ICommandHandler<TCommand>, CommandWithoutResponseAdapter<TCommand>>();
                }

                _ = services.AddTransient<ICommandHandler<TCommand, TResponse>>(
                    p => new CommandHandlerProxy<TCommand, TResponse>(p, new InMemoryCommandTransport(p, metadata.HandlerType), pipelineConfigurationAction));
            }

            void RegisterCustomInterface()
            {
                if (GetCustomCommandHandlerInterfaceType() is { } customInterfaceType)
                {
                    var plainHandlerInterface = typeof(TResponse) == typeof(UnitCommandResponse) ? typeof(ICommandHandler<TCommand>) : typeof(ICommandHandler<TCommand, TResponse>);
                    var dynamicType = DynamicType.Create(customInterfaceType, plainHandlerInterface);
                    services.TryAddTransient(customInterfaceType, dynamicType);
                }
            }

            Action<ICommandPipelineBuilder>? GetPipelineConfigurationAction()
            {
                var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is CommandHandlerPipelineConfiguration c && c.HandlerType == typeof(THandler))
                                                    ?.ImplementationInstance as CommandHandlerPipelineConfiguration;

                return existingConfiguration?.Configure ?? CreatePipelineConfigurationFunction();
            }

            static Action<ICommandPipelineBuilder>? CreatePipelineConfigurationFunction()
            {
                if (!typeof(THandler).IsAssignableTo(typeof(IConfigureCommandPipeline)))
                {
                    return null;
                }

#if NET7_0_OR_GREATER
                var pipelineConfigurationMethod = typeof(THandler).GetInterfaceMap(typeof(IConfigureCommandPipeline)).TargetMethods.Single();
#else
                const string configurationMethodName = "ConfigurePipeline";

                var pipelineConfigurationMethod = typeof(THandler).GetMethod(configurationMethodName, BindingFlags.Public | BindingFlags.Static);

                if (pipelineConfigurationMethod is null)
                {
                    throw new InvalidOperationException(
                        $"command handler type '{typeof(THandler).Name}' implements the interface '{nameof(IConfigureCommandPipeline)}' but does not have a public method '{configurationMethodName}'");
                }

                var methodHasInvalidReturnType = pipelineConfigurationMethod.ReturnType != typeof(void);
                var methodHasInvalidParameterTypes = pipelineConfigurationMethod.GetParameters().Length != 1
                                                     || pipelineConfigurationMethod.GetParameters().Single().ParameterType != typeof(ICommandPipelineBuilder);

                if (methodHasInvalidReturnType || methodHasInvalidParameterTypes)
                {
                    throw new InvalidOperationException(
                        $"command handler type '{typeof(THandler).Name}' has an invalid method signature for '{configurationMethodName}'; ensure that the signature is 'public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)'");
                }
#endif

                var builderParam = Expression.Parameter(typeof(ICommandPipelineBuilder));
                var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
                var lambda = Expression.Lambda(body, builderParam).Compile();
                return (Action<ICommandPipelineBuilder>)lambda;
            }

            static Type? GetCustomCommandHandlerInterfaceType()
            {
                var interfaces = typeof(THandler).GetInterfaces()
                                                 .Where(i => i.IsCustomCommandHandlerInterfaceType<TCommand, TResponse>())
                                                 .ToList();

                if (interfaces.Count < 1)
                {
                    return null;
                }

                if (interfaces.Count > 1)
                {
                    throw new InvalidOperationException($"command handler type '{typeof(THandler).Name}' implements more than one custom interface for command '{typeof(TCommand).Name}'");
                }

                var customHandlerInterface = interfaces.Single();

                if (customHandlerInterface.AllMethods().Count() > 1)
                {
                    throw new ArgumentException(
                        $"command handler type '{typeof(THandler).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom command handler interface types are not allowed to have any additional methods beside the '{nameof(ICommandHandler<object>.ExecuteCommand)}' inherited from '{typeof(ICommandHandler<>).Name}'");
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

            var configurationMethod = typeof(CommandServiceCollectionConfigurator).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

            if (configurationMethod == null)
            {
                throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
            }

            foreach (var middlewareType in middlewareTypes)
            {
                var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullMiddlewareConfiguration));

                try
                {
                    _ = genericConfigurationMethod.Invoke(null, new object[] { services });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            _ = services.AddSingleton<IReadOnlyDictionary<Type, ICommandMiddlewareInvoker>>(p => p.GetRequiredService<IEnumerable<ICommandMiddlewareInvoker>>().ToDictionary(i => i.MiddlewareType));
        }

        private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
        {
            _ = services.AddSingleton(new CommandMiddlewareMetadata(typeof(TMiddleware), GetMiddlewareConfigurationType(typeof(TMiddleware))));
            _ = services.AddSingleton<ICommandMiddlewareInvoker, CommandMiddlewareInvoker<TMiddleware, TConfiguration>>();
        }

        private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsCommandMiddlewareInterface).GetGenericArguments().FirstOrDefault();

        private static bool HasCommandMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsCommandMiddlewareInterface);

        private static bool IsCommandMiddlewareInterface(Type i) => i == typeof(ICommandMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>));
    }
}
