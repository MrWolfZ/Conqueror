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
    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler<THandler>(ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services, ServiceLifetime serviceLifetime)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(typeof(THandler), ServiceDescriptor.Describe(typeof(THandler), typeof(THandler), serviceLifetime));
    }

    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                          Func<IServiceProvider, THandler> factory)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(factory, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                          Func<IServiceProvider, THandler> factory,
                                                                          ServiceLifetime serviceLifetime)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(typeof(THandler), ServiceDescriptor.Describe(typeof(THandler), factory, serviceLifetime));
    }

    public static IServiceCollection AddConquerorCommandHandler<THandler>(this IServiceCollection services,
                                                                          THandler instance)
        where THandler : class, ICommandHandler
    {
        return services.AddConquerorCommandHandler(typeof(THandler), ServiceDescriptor.Singleton(typeof(THandler), instance));
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand, TResponse>(this IServiceCollection services,
                                                                                             Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn)
        where TCommand : class
    {
        return services.AddConquerorCommandHandler(p => new DelegateCommandHandler<TCommand, TResponse>(handlerFn, p));
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand, TResponse>(this IServiceCollection services,
                                                                                             Func<TCommand, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
                                                                                             Action<ICommandPipeline<TCommand, TResponse>> configurePipeline)
        where TCommand : class
    {
        var handlerType = typeof(DelegateCommandHandler<TCommand, TResponse>);
        services.Replace(ServiceDescriptor.Transient(p => new DelegateCommandHandler<TCommand, TResponse>(handlerFn, p)));
        return services.AddConquerorCommandHandler(handlerType, configurePipeline);
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand>(this IServiceCollection services,
                                                                                  Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn)
        where TCommand : class
    {
        return services.AddConquerorCommandHandler(p => new DelegateCommandHandler<TCommand>(handlerFn, p));
    }

    public static IServiceCollection AddConquerorCommandHandlerDelegate<TCommand>(this IServiceCollection services,
                                                                                  Func<TCommand, IServiceProvider, CancellationToken, Task> handlerFn,
                                                                                  Action<ICommandPipeline<TCommand>> configurePipeline)
        where TCommand : class
    {
        var handlerType = typeof(DelegateCommandHandler<TCommand>);
        services.Replace(ServiceDescriptor.Transient(p => new DelegateCommandHandler<TCommand>(handlerFn, p)));
        return services.AddConquerorCommandHandler(handlerType, configurePipeline);
    }

    internal static void TryAddConquerorCommandHandler(this IServiceCollection services,
                                                       Type handlerType,
                                                       ServiceDescriptor serviceDescriptor)
    {
        services.TryAdd(serviceDescriptor);
        services.AddConquerorCommandHandler(handlerType, (Delegate?)null);
    }

    private static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 ServiceDescriptor serviceDescriptor)
    {
        services.Replace(serviceDescriptor);
        return services.AddConquerorCommandHandler(handlerType, (Delegate?)null);
    }

    private static IServiceCollection AddConquerorCommandHandler(this IServiceCollection services,
                                                                 Type handlerType,
                                                                 Delegate? configurePipeline)
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
                                                                                Delegate? configurePipeline)
        where THandler : class, ICommandHandler
        where TCommand : class
    {
        var isWithoutResponse = typeof(THandler).IsAssignableTo(typeof(ICommandHandler<TCommand>));

        Action<ICommandPipeline<TCommand, TResponse>>? pipelineConfigurationAction = null;

        if (isWithoutResponse)
        {
            var configureWithoutResponse = (Action<ICommandPipeline<TCommand>>?)configurePipeline
                                           ?? CreatePipelineWithoutResponseConfigurationFunction(typeof(THandler));

            if (configureWithoutResponse is not null)
            {
                pipelineConfigurationAction = p =>
                {
                    var adapter = new CommandPipelineWithoutResponseAdapter<TCommand>((ICommandPipeline<TCommand, UnitCommandResponse>)p);
                    configureWithoutResponse(adapter);
                };
            }
        }
        else
        {
            pipelineConfigurationAction = (Action<ICommandPipeline<TCommand, TResponse>>?)configurePipeline
                                          ?? CreatePipelineConfigurationFunction(typeof(THandler));
        }

        var existingRegistrations = services.Select(d => d.ImplementationInstance)
                                            .OfType<CommandHandlerRegistrationInternal>()
                                            .ToDictionary(r => r.CommandType);

        if (existingRegistrations.TryGetValue(typeof(TCommand), out var existingRegistration))
        {
            if (typeof(THandler) != existingRegistration.HandlerType
                || existingRegistration.HandlerType == typeof(DelegateCommandHandler<TCommand, TResponse>)
                || existingRegistration.HandlerType == typeof(DelegateCommandHandler<TCommand>))
            {
                throw new InvalidOperationException($"attempted to register handler type '{typeof(THandler)}' for command type '{typeof(TCommand)}', but handler type '{existingRegistration.HandlerType}' is already registered.");
            }
        }
        else
        {
            var responseType = isWithoutResponse ? null : typeof(TResponse);
            var registration = new CommandHandlerRegistrationInternal(typeof(TCommand), responseType, typeof(THandler), pipelineConfigurationAction);
            services.AddSingleton(registration);
        }

        services.AddConquerorCommandClient<THandler>(new InProcessCommandTransport(typeof(THandler), pipelineConfigurationAction));

        return services;

        static Action<ICommandPipeline<TCommand, TResponse>>? CreatePipelineConfigurationFunction(Type handlerType)
        {
            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(ICommandHandler<TCommand, TResponse>))
                                                         .TargetMethods
                                                         .Where(m => m.DeclaringType == handlerType)
                                                         .SingleOrDefault(m => m.Name == nameof(ICommandHandler<TCommand, TResponse>.ConfigurePipeline));

            if (pipelineConfigurationMethod is null)
            {
                return null;
            }

            var builderParam = Expression.Parameter(typeof(ICommandPipeline<TCommand, TResponse>));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<ICommandPipeline<TCommand, TResponse>>)lambda;
        }

        static Action<ICommandPipeline<TCommand>>? CreatePipelineWithoutResponseConfigurationFunction(Type handlerType)
        {
            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(ICommandHandler<TCommand>))
                                                         .TargetMethods
                                                         .Where(m => m.DeclaringType == handlerType)
                                                         .SingleOrDefault(m => m.Name == nameof(ICommandHandler<TCommand>.ConfigurePipeline));

            if (pipelineConfigurationMethod is null)
            {
                return null;
            }

            var builderParam = Expression.Parameter(typeof(ICommandPipeline<TCommand>));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<ICommandPipeline<TCommand>>)lambda;
        }
    }
}
