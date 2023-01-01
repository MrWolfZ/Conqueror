using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandRegistrationFinalizer : IConquerorRegistrationFinalizer
    {
        private readonly IServiceCollection services;

        public CommandRegistrationFinalizer(IServiceCollection services)
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
                                       .Where(t => t.IsAssignableTo(typeof(ICommandHandler)))
                                       .Distinct()
                                       .ToList();

            var registrations = new List<CommandHandlerRegistration>();

            foreach (var handlerType in handlerTypes)
            {
                var pipelineConfigurationAction = GetPipelineConfigurationAction(handlerType);

                services.AddConquerorCommandClient(handlerType, b => new InMemoryCommandTransport(b.ServiceProvider, handlerType), pipelineConfigurationAction);

                foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
                {
                    registrations.Add(new(commandType, responseType, handlerType));
                }
            }

            var duplicateRegistrations = registrations.GroupBy(t => t.CommandType).FirstOrDefault(g => g.Count() > 1);

            if (duplicateRegistrations is not null)
            {
                var commandType = duplicateRegistrations.Key;
                var duplicateHandlerTypes = duplicateRegistrations.Select(h => h.HandlerType);
                throw new InvalidOperationException($"only a single handler for command type {commandType} is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
            }

            _ = services.AddSingleton(new CommandHandlerRegistry(registrations))
                        .AddSingleton<ICommandHandlerRegistry>(p => p.GetRequiredService<CommandHandlerRegistry>());

            Action<ICommandPipelineBuilder>? GetPipelineConfigurationAction(Type handlerType)
            {
                var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is CommandHandlerPipelineConfiguration c && c.HandlerType == handlerType)
                                                    ?.ImplementationInstance as CommandHandlerPipelineConfiguration;

                return existingConfiguration?.Configure ?? CreatePipelineConfigurationFunction(handlerType);
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

            var configurationMethod = typeof(CommandRegistrationFinalizer).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

            if (configurationMethod == null)
            {
                throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
            }

            var registrations = new List<CommandMiddlewareRegistration>();

            foreach (var middlewareType in middlewareTypes)
            {
                registrations.Add(new(middlewareType, GetMiddlewareConfigurationType(middlewareType)));

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

            _ = services.AddSingleton(new CommandMiddlewareRegistry(registrations))
                        .AddSingleton<ICommandMiddlewareRegistry>(p => p.GetRequiredService<CommandMiddlewareRegistry>());

            _ = services.AddSingleton<IReadOnlyDictionary<Type, ICommandMiddlewareInvoker>>(p => p.GetRequiredService<IEnumerable<ICommandMiddlewareInvoker>>().ToDictionary(i => i.MiddlewareType));
        }

        private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
        {
            _ = services.AddSingleton<ICommandMiddlewareInvoker, CommandMiddlewareInvoker<TMiddleware, TConfiguration>>();
        }

        private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsCommandMiddlewareInterface).GetGenericArguments().FirstOrDefault();

        private static bool HasCommandMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsCommandMiddlewareInterface);

        private static bool IsCommandMiddlewareInterface(Type i) => i == typeof(ICommandMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>));
    }
}
