using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.CQS.CommandHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsCommandHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                          ServiceLifetime lifetime = ServiceLifetime.Transient)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
    }

    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                          Func<IServiceProvider, THandler> factory,
                                                                          ServiceLifetime lifetime = ServiceLifetime.Transient)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), factory, lifetime));
    }

    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                          THandler instance)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), instance));
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand, TResponse>(this IServiceCollection services,
                                                                                             Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn)
        where TCommand : class
    {
        return services.AddConquerorCommandHandler(p => new DelegateCommandHandler<TCommand, TResponse>(handlerFn, p));
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand, TResponse>(this IServiceCollection services,
                                                                                             Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
                                                                                             Action<ICommandPipelineBuilder> configurePipeline)
        where TCommand : class
    {
        return services.AddConquerorCommandHandler(typeof(DelegateCommandHandler<TCommand, TResponse>),
                                                   ServiceDescriptor.Transient(p => new DelegateCommandHandler<TCommand, TResponse>(handlerFn, p)),
                                                   configurePipeline);
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand>(this IServiceCollection services,
                                                                                  Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn)
        where TCommand : class
    {
        return services.AddConquerorCommandHandler(p => new DelegateCommandHandler<TCommand>(handlerFn, p));
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand>(this IServiceCollection services,
                                                                                  Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn,
                                                                                  Action<ICommandPipelineBuilder> configurePipeline)
        where TCommand : class
    {
        return services.AddConquerorCommandHandler(typeof(DelegateCommandHandler<TCommand>),
                                                   ServiceDescriptor.Transient(p => new DelegateCommandHandler<TCommand>(handlerFn, p)),
                                                   configurePipeline);
    }

    internal static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                  Type handlerType,
                                                                  ServiceDescriptor serviceDescriptor,
                                                                  Action<ICommandPipelineBuilder>? configurePipeline = null)
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorCommandHandler(handlerType, configurePipeline);
    }

    private static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 Action<ICommandPipelineBuilder>? configurePipeline)
    {
        handlerType.ValidateNoInvalidCommandHandlerInterface();

        var addHandlerMethod = typeof(ConquerorCqsCommandHandlerServiceCollectionExtensions).GetMethod(nameof(AddHandler), BindingFlags.NonPublic | BindingFlags.Static);

        if (addHandlerMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddHandler)}'");
        }

        foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
        {
            var genericAddMethod = addHandlerMethod.MakeGenericMethod(handlerType, commandType, responseType ?? typeof(UnitCommandResponse));

            try
            {
                _ = genericAddMethod.Invoke(null, [services, configurePipeline]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static IServiceCollection AddHandler<THandler, TCommand, TResponse>(this IServiceCollection services,
                                                                                Action<ICommandPipelineBuilder>? configurePipeline)
        where TCommand : class
    {
        var existingRegistrations = services.Where(d => d.ImplementationInstance is CommandHandlerRegistration)
                                            .ToDictionary(d => ((CommandHandlerRegistration)d.ImplementationInstance!).CommandType);

        if (existingRegistrations.TryGetValue(typeof(TCommand), out var existingDescriptor))
        {
            if (typeof(THandler) != ((CommandHandlerRegistration)existingDescriptor.ImplementationInstance!).HandlerType)
            {
                services.Remove(existingDescriptor);
                var responseType = typeof(TResponse) == typeof(UnitCommandResponse) ? null : typeof(TResponse);
                var registration = new CommandHandlerRegistration(typeof(TCommand), responseType, typeof(THandler));
                services.AddSingleton(registration);
            }
        }
        else
        {
            var responseType = typeof(TResponse) == typeof(UnitCommandResponse) ? null : typeof(TResponse);
            var registration = new CommandHandlerRegistration(typeof(TCommand), responseType, typeof(THandler));
            services.AddSingleton(registration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(typeof(THandler));

        services.AddConquerorCommandClient(typeof(THandler), new InMemoryCommandTransport(typeof(THandler)), pipelineConfigurationAction);

        return services;

        static Action<ICommandPipelineBuilder> CreatePipelineConfigurationFunction(Type handlerType)
        {
            var pipelineConfigurationMethod = handlerType.IsAssignableTo(typeof(ICommandHandler<TCommand>))
                ? handlerType.GetInterfaceMap(typeof(ICommandHandler<TCommand>)).TargetMethods.Single(m => m.Name == nameof(ICommandHandler<TCommand>.ConfigurePipeline))
                : handlerType.GetInterfaceMap(typeof(ICommandHandler<TCommand, TResponse>)).TargetMethods.Single(m => m.Name == nameof(ICommandHandler<TCommand, TResponse>.ConfigurePipeline));

            var builderParam = Expression.Parameter(typeof(ICommandPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<ICommandPipelineBuilder>)lambda;
        }
    }
}
