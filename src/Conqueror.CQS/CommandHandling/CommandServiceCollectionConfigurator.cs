using System;
using System.Linq;
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
                                       .OfType<Type>()
                                       .Where(t => t.IsAssignableTo(typeof(ICommandHandler)))
                                       .Distinct()
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidCommandHandlerInterface();
                RegisterMetadata(handlerType);
                RegisterPlainInterfaces(handlerType);
                RegisterCustomerInterfaces(handlerType);
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

            void RegisterCustomerInterfaces(Type handlerType)
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
        }

        private static void ConfigureMiddlewares(IServiceCollection services)
        {
            var middlewareTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                          .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                          .OfType<Type>()
                                          .Where(t => t.IsAssignableTo(typeof(ICommandMiddleware)))
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
                        throw new ArgumentException($"type {middlewareType.Name} implements {typeof(ICommandMiddleware<>).Name} more than once");
                }
            }

            foreach (var middlewareType in middlewareTypes)
            {
                RegisterMetadata(middlewareType);
                RegisterInvoker(middlewareType);
            }

            void RegisterMetadata(Type middlewareType)
            {
                var attributeType = middlewareType.GetInterfaces().First(IsCommandMiddlewareInterface).GetGenericArguments().First();

                _ = services.AddSingleton(new CommandMiddlewareMetadata(middlewareType, attributeType));
            }

            void RegisterInvoker(Type middlewareType)
            {
                var attributeType = middlewareType.GetInterfaces().First(IsCommandMiddlewareInterface).GetGenericArguments().First();

                _ = services.AddTransient(typeof(CommandMiddlewareInvoker<>).MakeGenericType(attributeType));
            }

            static bool IsCommandMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>);
        }
    }
}
