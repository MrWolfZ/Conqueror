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
            var handlerTypes = services.Where(d => d.ServiceType == d.ImplementationType)
                                       .Select(d => d.ImplementationType)
                                       .OfType<Type>()
                                       .Where(t => t.IsAssignableTo(typeof(ICommandHandler)))
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidCommandHandlerInterface();
                
                foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
                {
                    var metadata = new CommandHandlerMetadata(commandType, responseType, handlerType);

                    _ = services.AddSingleton(metadata);
                }
                
                var customInterfaceTypes = handlerType.GetCustomCommandHandlerInterfaceTypes();

                foreach (var customInterfaceType in customInterfaceTypes)
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
