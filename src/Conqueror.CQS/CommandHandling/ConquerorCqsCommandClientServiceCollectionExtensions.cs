using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
                                                                         Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandClient(typeof(THandler), transportClientFactory);
    }

    public static IServiceCollection AddConquerorCommandClient<THandler>(this IServiceCollection services,
                                                                         Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandClient(typeof(THandler), transportClientFactory);
    }

    internal static IServiceCollection AddConquerorCommandClient<THandler>(this IServiceCollection services,
                                                                           ICommandTransportClient transportClient)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandClient(typeof(THandler), new(transportClient), null);
    }

    private static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                Type handlerType,
                                                                Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
    {
        return services.AddConquerorCommandClient(handlerType, new(transportClientFactory), null);
    }

    private static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                Type handlerType,
                                                                Func<ICommandTransportClientBuilder, Task<ICommandTransportClient>> transportClientFactory)
    {
        return services.AddConquerorCommandClient(handlerType, new(transportClientFactory), null);
    }

    private static IServiceCollection AddConquerorCommandClient(this IServiceCollection services,
                                                                Type handlerType,
                                                                CommandTransportClientFactory transportClientFactory,
                                                                Delegate? configurePipeline)
    {
        handlerType.ValidateNoInvalidCommandHandlerInterface();

        var addClientMethod = typeof(ConquerorCqsCommandClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

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
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddClient<THandler, TCommand, TResponse>(this IServiceCollection services,
                                                                 CommandTransportClientFactory transportClientFactory,
                                                                 Action<ICommandPipeline<TCommand, TResponse>>? configurePipeline)
        where THandler : class, ICommandHandler
        where TCommand : class
    {
        typeof(THandler).ValidateNoInvalidCommandHandlerInterface();

        var existingCommandRegistrations = services.Select(d => d.ServiceType)
                                                   .Where(t => t.IsCommandHandlerInterfaceType())
                                                   .SelectMany(t => t.GetCommandAndResponseTypes())

                                                   // filter out the proxy registrations for handlers without response so that we only
                                                   // get the response-less handler registrations
                                                   .Where(t => t.ResponseType != typeof(UnitCommandResponse))
                                                   .ToDictionary(t => t.CommandType, t => t.ResponseType);

        if (existingCommandRegistrations.TryGetValue(typeof(TCommand), out var existingResponseType) && typeof(TResponse) != (existingResponseType ?? typeof(UnitCommandResponse)))
        {
            throw new InvalidOperationException($"client for command type '{typeof(TCommand)}' is already registered with response type '{existingResponseType}', but tried to add client with different response type '{typeof(TResponse)}'");
        }

        services.AddConquerorCqsCommandServices();

        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                services.TryAddTransient<ICommandHandler<TCommand>, CommandHandlerWithoutResponseAdapter<TCommand>>();
            }

            _ = services.Replace(ServiceDescriptor.Transient<ICommandHandler<TCommand, TResponse>>(CreateProxy));
        }

        CommandHandlerProxy<TCommand, TResponse> CreateProxy(IServiceProvider serviceProvider)
        {
            return new(serviceProvider, transportClientFactory, configurePipeline);
        }

        void RegisterCustomInterface()
        {
            if (GetCustomCommandHandlerInterfaceType() is { } customInterfaceType)
            {
                var plainHandlerInterface = typeof(TResponse) == typeof(UnitCommandResponse) ? typeof(ICommandHandler<TCommand>) : typeof(ICommandHandler<TCommand, TResponse>);
                var baseType = typeof(TResponse) == typeof(UnitCommandResponse) ? typeof(CommandHandlerGeneratedProxyBase<TCommand>) : typeof(CommandHandlerGeneratedProxyBase<TCommand, TResponse>);
                var proxyType = ProxyTypeGenerator.Create(customInterfaceType, plainHandlerInterface, baseType);
                services.TryAddTransient(customInterfaceType, proxyType);
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
                throw new ArgumentException($"command handler type '{typeof(THandler).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom command handler interface types are not allowed to have any additional methods beside the '{nameof(ICommandHandler<object>.ExecuteCommand)}' inherited from '{typeof(ICommandHandler<>).Name}'");
            }

            return customHandlerInterface;
        }
    }
}
