using System;
using System.Linq;
using Conqueror.Common;
using Conqueror.Streaming.Interactive.Common;
using Microsoft.Extensions.DependencyInjection;
#if !NET7_0_OR_GREATER
#endif

namespace Conqueror.Streaming.Interactive
{
    internal sealed class InteractiveStreamingRegistrationFinalizer : IConquerorRegistrationFinalizer
    {
        private readonly IServiceCollection services;

        public InteractiveStreamingRegistrationFinalizer(IServiceCollection services)
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
                                       .Where(t => t.IsAssignableTo(typeof(IInteractiveStreamingHandler)))
                                       .Distinct()
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidInteractiveStreamingHandlerInterface();
                RegisterHandlerMetadata(handlerType);
                RegisterPlainInterfaces(handlerType);
                RegisterCustomInterfaces(handlerType);
                //// TODO
                // RegisterPipelineConfiguration(handlerType);
            }

            ValidateNoDuplicateRequestTypes();

            void ValidateNoDuplicateRequestTypes()
            {
                var duplicateMetadata = services.Select(d => d.ImplementationInstance).OfType<InteractiveStreamingHandlerMetadata>().GroupBy(t => t.RequestType).FirstOrDefault(g => g.Count() > 1);

                if (duplicateMetadata is not null)
                {
                    var requestType = duplicateMetadata.Key;
                    var duplicateHandlerTypes = duplicateMetadata.Select(h => h.HandlerType);
                    throw new InvalidOperationException(
                        $"only a single handler for interactive streaming request type '{requestType}' is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
                }
            }

            void RegisterHandlerMetadata(Type handlerType)
            {
                foreach (var (requestType, itemType) in handlerType.GetInteractiveStreamingRequestAndItemTypes())
                {
                    _ = services.AddSingleton(new InteractiveStreamingHandlerMetadata(requestType, itemType, handlerType));
                }
            }

            void RegisterPlainInterfaces(Type handlerType)
            {
                foreach (var (requestType, itemType) in handlerType.GetInteractiveStreamingRequestAndItemTypes())
                {
                    _ = services.AddTransient(typeof(IInteractiveStreamingHandler<,>).MakeGenericType(requestType, itemType),
                                              typeof(InteractiveStreamingHandlerProxy<,>).MakeGenericType(requestType, itemType));
                }
            }

            void RegisterCustomInterfaces(Type handlerType)
            {
                foreach (var customInterfaceType in handlerType.GetCustomInteractiveStreamingHandlerInterfaceTypes())
                {
                    foreach (var plainInterfaceType in customInterfaceType.GetInteractiveStreamingHandlerInterfaceTypes())
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
//                 _ = services.ConfigureInteractiveStreamingPipeline(handlerType, configure);
//             }
//
//             static Action<IInteractiveStreamingPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
//             {
//                 if (!handlerType.IsAssignableTo(typeof(IConfigureInteractiveStreamingPipeline)))
//                 {
//                     return null;
//                 }
//
// #if NET7_0_OR_GREATER
//                 var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureInteractiveStreamingPipeline)).TargetMethods.Single();
// #else
//                 const string configurationMethodName = "ConfigurePipeline";
//
//                 var pipelineConfigurationMethod = handlerType.GetMethod(configurationMethodName, BindingFlags.Public | BindingFlags.Static);
//
//                 if (pipelineConfigurationMethod is null)
//                 {
//                     throw new InvalidOperationException(
//                         $"command handler type '{handlerType.Name}' implements the interface '{nameof(IConfigureInteractiveStreamingPipeline)}' but does not have a public method '{configurationMethodName}'");
//                 }
//
//                 var methodHasInvalidReturnType = pipelineConfigurationMethod.ReturnType != typeof(void);
//                 var methodHasInvalidParameterTypes = pipelineConfigurationMethod.GetParameters().Length != 1
//                                                      || pipelineConfigurationMethod.GetParameters().Single().ParameterType != typeof(IInteractiveStreamingPipelineBuilder);
//
//                 if (methodHasInvalidReturnType || methodHasInvalidParameterTypes)
//                 {
//                     throw new InvalidOperationException(
//                         $"command handler type '{handlerType.Name}' has an invalid method signature for '{configurationMethodName}'; ensure that the signature is 'public static void ConfigurePipeline(IInteractiveStreamingPipelineBuilder pipeline)'");
//                 }
// #endif
//
//                 var builderParam = Expression.Parameter(typeof(IInteractiveStreamingPipelineBuilder));
//                 var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
//                 var lambda = Expression.Lambda(body, builderParam).Compile();
//                 return (Action<IInteractiveStreamingPipelineBuilder>)lambda;
//             }
        }

        // private static void ConfigureMiddlewares(IServiceCollection services)
        // {
        //     var middlewareTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
        //                                   .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
        //                                   .Concat(services.Where(d => !d.ServiceType.IsInterface && !d.ServiceType.IsAbstract && d.ImplementationFactory != null).Select(d => d.ServiceType))
        //                                   .OfType<Type>()
        //                                   .Where(HasInteractiveStreamingMiddlewareInterface)
        //                                   .Distinct()
        //                                   .ToList();
        //
        //     foreach (var middlewareType in middlewareTypes)
        //     {
        //         var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsInteractiveStreamingMiddlewareInterface).ToList();
        //
        //         switch (middlewareInterfaces.Count)
        //         {
        //             case < 1:
        //                 continue;
        //
        //             case > 1:
        //                 throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IInteractiveStreamingMiddleware)} more than once");
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
        //         var configurationType = middlewareType.GetInterfaces().First(IsInteractiveStreamingMiddlewareInterface).GetGenericArguments().FirstOrDefault();
        //
        //         _ = services.AddSingleton(new InteractiveStreamingMiddlewareMetadata(middlewareType, configurationType));
        //     }
        //
        //     static bool HasInteractiveStreamingMiddlewareInterface(Type t) => t.GetInterfaces().Any(IsInteractiveStreamingMiddlewareInterface);
        //     static bool IsInteractiveStreamingMiddlewareInterface(Type i) => i == typeof(IInteractiveStreamingMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInteractiveStreamingMiddleware<>));
        // }
    }
}
