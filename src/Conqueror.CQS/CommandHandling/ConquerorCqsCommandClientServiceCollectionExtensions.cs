using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.CQS.CommandHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsCommandClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorCommandClient<THandler>(this IServiceCollection services,
                                                                         Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                                         Action<ICommandPipelineBuilder>? configurePipeline = null)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandClient(typeof(THandler), transportClientFactory, configurePipeline);
    }

    public static IServiceCollection AddConquerorCommandClient<THandler>(this IServiceCollection services,
                                                                         Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory,
                                                                         Action<ICommandPipelineBuilder>? configurePipeline = null)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandClient(typeof(THandler), transportClientFactory, configurePipeline);
    }

    internal static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 ICommandTransportClient transportClient,
                                                                 Action<ICommandPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorCommandClient(handlerType, new CommandTransportClientFactory(transportClient), configurePipeline);
    }

    internal static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                                 Action<ICommandPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorCommandClient(handlerType, new CommandTransportClientFactory(transportClientFactory), configurePipeline);
    }

    internal static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory,
                                                                 Action<ICommandPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorCommandClient(handlerType, new CommandTransportClientFactory(transportClientFactory), configurePipeline);
    }

    internal static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 CommandTransportClientFactory transportClientFactory,
                                                                 Action<ICommandPipelineBuilder>? configurePipeline)
    {
        handlerType.ValidateNoInvalidCommandHandlerInterface();

        services.AddConquerorCqsCommandServices();

        var addClientMethod = typeof(ConquerorCqsCommandClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

        if (addClientMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
        }

        var existingCommandRegistrations = services.Select(d => d.ServiceType)
                                                   .Where(t => t.IsCommandHandlerInterfaceType())
                                                   .SelectMany(t => t.GetCommandAndResponseTypes())
                                                   .Where(t => t.ResponseType != typeof(UnitCommandResponse))
                                                   .ToDictionary(t => t.CommandType, t => t.ResponseType);

        foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
        {
            if (existingCommandRegistrations.TryGetValue(commandType, out var existingResponseType) && responseType != existingResponseType)
            {
                throw new InvalidOperationException($"client for command type '{commandType.Name}' is already registered with response type '{(existingResponseType ?? typeof(UnitCommandResponse)).Name}', but tried to add client with different response type '{(responseType ?? typeof(UnitCommandResponse)).Name}'");
            }

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

    private static void AddClient<THandler, TCommand, TResponse>(this IServiceCollection services,
                                                                 CommandTransportClientFactory transportClientFactory,
                                                                 Action<ICommandPipelineBuilder>? configurePipeline = null)
        where THandler : class, ICommandHandler
        where TCommand : class
    {
        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                services.TryAddTransient<ICommandHandler<TCommand>, CommandWithoutResponseAdapter<TCommand>>();
            }

            _ = services.Replace(ServiceDescriptor.Transient<ICommandHandler<TCommand, TResponse>>(p => new CommandHandlerProxy<TCommand, TResponse>(p, transportClientFactory, configurePipeline)));
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
