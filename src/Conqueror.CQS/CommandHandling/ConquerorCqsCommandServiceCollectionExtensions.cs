using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Common;
using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsCommandServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorCommandClient<THandler>(this IServiceCollection services,
                                                                             Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                                             Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler
        {
            return services.AddConquerorCommandClient(typeof(THandler), transportClientFactory, configurePipeline);
        }

        internal static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                     Type handlerType,
                                                                     Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                                     Action<ICommandPipelineBuilder>? configurePipeline)
        {
            handlerType.ValidateNoInvalidCommandHandlerInterface();

            services.AddConquerorCqsCommandServices();

            var addClientMethod = typeof(ConquerorCqsCommandServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

            if (addClientMethod == null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
            }

            foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
            {
                var genericAddClientMethod = addClientMethod.MakeGenericMethod(handlerType, commandType, responseType ?? typeof(UnitCommandResponse));

                try
                {
                    _ = genericAddClientMethod.Invoke(null, new object?[] { services, transportClientFactory, configurePipeline });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            return services;
        }

        internal static void AddConquerorCqsCommandServices(this IServiceCollection services)
        {
            services.TryAddSingleton(new CommandRegistrationFinalizer(services));
            services.TryAddTransient<ICommandClientFactory, TransientCommandClientFactory>();
            services.TryAddSingleton<CommandClientFactory>();
            services.TryAddSingleton<CommandContextAccessor>();
            services.TryAddSingleton<ICommandContextAccessor>(p => p.GetRequiredService<CommandContextAccessor>());
        }

        internal static IServiceCollection ConfigureCommandPipeline<TCommandHandler>(this IServiceCollection services, Action<ICommandPipelineBuilder> configure)
            where TCommandHandler : ICommandHandler =>
            services.ConfigureCommandPipeline(typeof(TCommandHandler), configure);

        internal static IServiceCollection ConfigureCommandPipeline(this IServiceCollection services, Type handlerType, Action<ICommandPipelineBuilder> configure)
        {
            var existingConfiguration = services.FirstOrDefault(d => d.ImplementationInstance is CommandHandlerPipelineConfiguration c && c.HandlerType == handlerType);

            if (existingConfiguration is not null)
            {
                services.Remove(existingConfiguration);
            }

            services.AddSingleton(new CommandHandlerPipelineConfiguration(handlerType, configure));

            return services;
        }

        private static void AddClient<THandler, TCommand, TResponse>(this IServiceCollection services,
                                                                     Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                                     Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler
            where TCommand : class
        {
            RegisterPlainInterface();
            RegisterCustomInterface();

            void RegisterPlainInterface()
            {
                if (services.Any(d => d.ServiceType == typeof(ICommandHandler<TCommand, TResponse>)))
                {
                    throw new InvalidOperationException($"command client for handler type '{typeof(THandler)}' is already registered");
                }

                if (typeof(TResponse) == typeof(UnitCommandResponse))
                {
                    _ = services.AddTransient<ICommandHandler<TCommand>, CommandWithoutResponseAdapter<TCommand>>();
                }

                _ = services.AddTransient<ICommandHandler<TCommand, TResponse>>(
                    p => new CommandHandlerProxy<TCommand, TResponse>(p, transportClientFactory, configurePipeline));
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

            static Type? GetCustomCommandHandlerInterfaceType()
            {
                var interfaces = typeof(THandler).GetInterfaces()
                                                 .Concat(new[] { typeof(THandler) })
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
    }
}
