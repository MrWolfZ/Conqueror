using System;
using System.Linq;
using Conqueror.Common;
using Conqueror.Streaming.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamingRegistrationFinalizer : IConquerorRegistrationFinalizer
{
    private readonly IServiceCollection services;

    public StreamingRegistrationFinalizer(IServiceCollection services)
    {
        this.services = services;
    }

    public int ExecutionPhase => 1;

    public void Execute()
    {
        ConfigureHandlers(services);

        //// TODO
        //// ConfigureMiddlewares(services);
    }

    private static void ConfigureHandlers(IServiceCollection services)
    {
        var handlerTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                   .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                   .Concat(services.Where(d => !d.ServiceType.IsInterface && !d.ServiceType.IsAbstract && d.ImplementationFactory != null).Select(d => d.ServiceType))
                                   .OfType<Type>()
                                   .Where(t => t.IsAssignableTo(typeof(IStreamingHandler)))
                                   .Distinct()
                                   .ToList();

        foreach (var handlerType in handlerTypes)
        {
            handlerType.ValidateNoInvalidStreamingHandlerInterface();
            RegisterHandlerMetadata(handlerType);
            RegisterPlainInterfaces(handlerType);
            RegisterCustomInterfaces(handlerType);
            //// TODO
            // RegisterPipelineConfiguration(handlerType);
        }

        ValidateNoDuplicateRequestTypes();

        void ValidateNoDuplicateRequestTypes()
        {
            var duplicateMetadata = services.Select(d => d.ImplementationInstance).OfType<StreamingHandlerMetadata>().GroupBy(t => t.RequestType).FirstOrDefault(g => g.Count() > 1);

            if (duplicateMetadata is not null)
            {
                var requestType = duplicateMetadata.Key;
                var duplicateHandlerTypes = duplicateMetadata.Select(h => h.HandlerType);
                throw new InvalidOperationException(
                    $"only a single handler for streaming request type '{requestType}' is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
            }
        }

        void RegisterHandlerMetadata(Type handlerType)
        {
            foreach (var (requestType, itemType) in handlerType.GetStreamingRequestAndItemTypes())
            {
                _ = services.AddSingleton(new StreamingHandlerMetadata(requestType, itemType, handlerType));
            }
        }

        void RegisterPlainInterfaces(Type handlerType)
        {
            foreach (var (requestType, itemType) in handlerType.GetStreamingRequestAndItemTypes())
            {
                _ = services.AddTransient(typeof(IStreamingHandler<,>).MakeGenericType(requestType, itemType),
                                          typeof(StreamingHandlerProxy<,>).MakeGenericType(requestType, itemType));
            }
        }

        void RegisterCustomInterfaces(Type handlerType)
        {
            foreach (var customInterfaceType in handlerType.GetCustomStreamingHandlerInterfaceTypes())
            {
                foreach (var plainInterfaceType in customInterfaceType.GetStreamingHandlerInterfaceTypes())
                {
                    var dynamicType = DynamicType.Create(customInterfaceType, plainInterfaceType);
                    _ = services.AddTransient(customInterfaceType, dynamicType);
                }
            }
        }

        // TODO
//             void RegisterPipelineConfiguration(Type handlerType)
//             {
//                 var configure = CreatePipelineConfigurationFunction(handlerType);
//
//                 if (configure is null)
//                 {
//                     return;
//                 }
//
//                 _ = services.ConfigureStreamingPipeline(handlerType, configure);
//             }
//
//             static Action<IStreamingPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
//             {
//                 if (!handlerType.IsAssignableTo(typeof(IConfigureStreamingPipeline)))
//                 {
//                     return null;
//                 }
//
// #if NET7_0_OR_GREATER
//                 var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureStreamingPipeline)).TargetMethods.Single();
// #else
//                 const string configurationMethodName = "ConfigurePipeline";
//
//                 var pipelineConfigurationMethod = handlerType.GetMethod(configurationMethodName, BindingFlags.Public | BindingFlags.Static);
//
//                 if (pipelineConfigurationMethod is null)
//                 {
//                     throw new InvalidOperationException(
//                         $"command handler type '{handlerType.Name}' implements the interface '{nameof(IConfigureStreamingPipeline)}' but does not have a public method '{configurationMethodName}'");
//                 }
//
//                 var methodHasInvalidReturnType = pipelineConfigurationMethod.ReturnType != typeof(void);
//                 var methodHasInvalidParameterTypes = pipelineConfigurationMethod.GetParameters().Length != 1
//                                                      || pipelineConfigurationMethod.GetParameters().Single().ParameterType != typeof(IStreamingPipelineBuilder);
//
//                 if (methodHasInvalidReturnType || methodHasInvalidParameterTypes)
//                 {
//                     throw new InvalidOperationException(
//                         $"command handler type '{handlerType.Name}' has an invalid method signature for '{configurationMethodName}'; ensure that the signature is 'public static void ConfigurePipeline(IStreamingPipelineBuilder pipeline)'");
//                 }
// #endif
//
//                 var builderParam = Expression.Parameter(typeof(IStreamingPipelineBuilder));
//                 var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
//                 var lambda = Expression.Lambda(body, builderParam).Compile();
//                 return (Action<IStreamingPipelineBuilder>)lambda;
//             }
    }

    // private static void ConfigureMiddlewares(IServiceCollection services)
    // {
    //     var middlewareTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
    //                                   .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
    //                                   .Concat(services.Where(d => !d.ServiceType.IsInterface && !d.ServiceType.IsAbstract && d.ImplementationFactory != null).Select(d => d.ServiceType))
    //                                   .OfType<Type>()
    //                                   .Where(HasStreamingMiddlewareInterface)
    //                                   .Distinct()
    //                                   .ToList();
    //
    //     foreach (var middlewareType in middlewareTypes)
    //     {
    //         var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsStreamingMiddlewareInterface).ToList();
    //
    //         switch (middlewareInterfaces.Count)
    //         {
    //             case < 1:
    //                 continue;
    //
    //             case > 1:
    //                 throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IStreamingMiddleware)} more than once");
    //         }
    //     }
    //
    //     foreach (var middlewareType in middlewareTypes)
    //     {
    //         RegisterMetadata(middlewareType);
    //     }
    //
    //     void RegisterMetadata(Type middlewareType)
    //     {
    //         var configurationType = middlewareType.GetInterfaces().First(IsStreamingMiddlewareInterface).GetGenericArguments().FirstOrDefault();
    //
    //         _ = services.AddSingleton(new StreamingMiddlewareMetadata(middlewareType, configurationType));
    //     }
    //
    //     static bool HasStreamingMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsStreamingMiddlewareInterface);
    //     static bool IsStreamingMiddlewareInterface(Type i) => i == typeof(IStreamingMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamingMiddleware<>));
    // }
}
