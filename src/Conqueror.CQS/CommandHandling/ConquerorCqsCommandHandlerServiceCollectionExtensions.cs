using System;
using System.Linq;
using System.Linq.Expressions;
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

    internal static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                  Type handlerType,
                                                                  Action<ICommandPipelineBuilder>? configurePipeline)
    {
        var existingRegistrations = services.Where(d => d.ImplementationInstance is CommandHandlerRegistration)
                                            .ToDictionary(d => ((CommandHandlerRegistration)d.ImplementationInstance!).CommandType);

        foreach (var (commandType, responseType) in handlerType.GetCommandAndResponseTypes())
        {
            if (existingRegistrations.TryGetValue(commandType, out var existingDescriptor))
            {
                if (handlerType == ((CommandHandlerRegistration)existingDescriptor.ImplementationInstance!).HandlerType)
                {
                    continue;
                }

                services.Remove(existingDescriptor);
            }

            var registration = new CommandHandlerRegistration(commandType, responseType, handlerType);
            services.AddSingleton(registration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(handlerType);

        services.AddConquerorCommandClient(handlerType, new InMemoryCommandTransport(handlerType), pipelineConfigurationAction);

        return services;

        static Action<ICommandPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
        {
            if (!handlerType.IsAssignableTo(typeof(IConfigureCommandPipeline)))
            {
                return null;
            }

            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureCommandPipeline)).TargetMethods.Single();

            var builderParam = Expression.Parameter(typeof(ICommandPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<ICommandPipelineBuilder>)lambda;
        }
    }
}
