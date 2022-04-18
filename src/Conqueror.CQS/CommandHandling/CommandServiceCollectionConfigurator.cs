using System;
using System.Linq;
using Conqueror.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.CommandHandling
{
    internal sealed class CommandServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public void Configure(IServiceCollection services)
        {
            ConfigureHandlers(services);
            ConfigureMiddlewares(services);
        }

        private static void ConfigureHandlers(IServiceCollection services)
        {
            var handlerTypes = services.Where(d => d.ServiceType == d.ImplementationType)
                                       .Select(d => d.ImplementationType)
                                       .OfType<Type>()
                                       .Where(t => t.IsAssignableTo(typeof(ICommandHandler)))
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var (commandType, responseType) = handlerType.GetCommandAndResponseType();

                var metadata = new CommandHandlerMetadata(commandType, responseType, handlerType);

                _ = services.AddSingleton(metadata);

                var customInterfaceType = handlerType.GetCustomCommandHandlerInterfaceType();
                var plainInterfaceType = handlerType.GetCommandHandlerInterfaceType();

                if (customInterfaceType is not null)
                {
                    var dynamicType = DynamicType.Create(customInterfaceType, plainInterfaceType);
                    _ = services.AddTransient(customInterfaceType, dynamicType);
                }
            }
        }

        private static void ConfigureMiddlewares(IServiceCollection services)
        {
            foreach (var middlewareType in services.Where(d => d.ServiceType == d.ImplementationType).Select(d => d.ImplementationType).OfType<Type>().ToList())
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

            static bool IsCommandMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>);
        }
    }
}
