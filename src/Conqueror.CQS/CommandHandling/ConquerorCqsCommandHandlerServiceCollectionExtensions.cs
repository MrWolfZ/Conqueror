using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Conqueror;
using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsCommandHandlerServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                              ServiceLifetime lifetime = ServiceLifetime.Transient)
            where THandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHandler(typeof(THandler), new(typeof(THandler), typeof(THandler), lifetime));
        }

        public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                              Func<IServiceProvider, THandler> factory,
                                                                              ServiceLifetime lifetime = ServiceLifetime.Transient)
            where THandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHandler(typeof(THandler), new(typeof(THandler), factory, lifetime));
        }

        public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                              THandler instance)
            where THandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHandler(typeof(THandler), new(typeof(THandler), instance));
        }

        public static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                    Type handlerType,
                                                                    ServiceDescriptor serviceDescriptor)
        {
            services.TryAdd(serviceDescriptor);
            return services.AddConquerorCommandHandler(handlerType);
        }

        internal static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                      Type handlerType)
        {
            var existingRegistrations = services.Select(d => d.ImplementationInstance).OfType<CommandHandlerRegistration>().ToDictionary(r => r.CommandType, r => r.HandlerType);

            foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
            {
                if (existingRegistrations.TryGetValue(commandType, out var existingHandlerType))
                {
                    if (existingHandlerType == handlerType)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"only a single handler for command type '{commandType.Name}' is allowed, but found multiple ('{existingHandlerType.Name}' and '{handlerType.Name}')");
                }

                var registration = new CommandHandlerRegistration(commandType, responseType, handlerType);
                services.AddSingleton(registration);
            }

            var pipelineConfigurationAction = CreatePipelineConfigurationFunction(handlerType);

            services.TryAddConquerorCommandClient(handlerType, b => new InMemoryCommandTransport(b.ServiceProvider, handlerType), pipelineConfigurationAction);

            return services;

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
    }
}
