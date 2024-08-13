using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingRequestClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamingRequestClient<THandler>(this IServiceCollection services,
                                                                                  Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                                  Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler
    {
        return services.AddConquerorStreamingRequestClient(typeof(THandler), transportClientFactory, configurePipeline);
    }

    public static IServiceCollection AddConquerorStreamingRequestClient<THandler>(this IServiceCollection services,
                                                                                  Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>> transportClientFactory,
                                                                                  Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler
    {
        return services.AddConquerorStreamingRequestClient(typeof(THandler), transportClientFactory, configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamingRequestClient(this IServiceCollection services,
                                                                          Type handlerType,
                                                                          IStreamingRequestTransportClient transportClient,
                                                                          Action<IStreamingRequestPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorStreamingRequestClient(handlerType, new StreamingRequestTransportClientFactory(transportClient), configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamingRequestClient(this IServiceCollection services,
                                                                          Type handlerType,
                                                                          Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                          Action<IStreamingRequestPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorStreamingRequestClient(handlerType, new StreamingRequestTransportClientFactory(transportClientFactory), configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamingRequestClient(this IServiceCollection services,
                                                                          Type handlerType,
                                                                          Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>> transportClientFactory,
                                                                          Action<IStreamingRequestPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorStreamingRequestClient(handlerType, new StreamingRequestTransportClientFactory(transportClientFactory), configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamingRequestClient(this IServiceCollection services,
                                                                          Type handlerType,
                                                                          StreamingRequestTransportClientFactory transportClientFactory,
                                                                          Action<IStreamingRequestPipelineBuilder>? configurePipeline)
    {
        handlerType.ValidateNoInvalidStreamingRequestHandlerInterface();

        services.AddConquerorStreaming();

        var addClientMethod = typeof(ConquerorStreamingRequestClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

        if (addClientMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
        }

        var existingStreamingRequestRegistrations = services.Select(d => d.ServiceType)
                                                            .Where(t => t.IsStreamingRequestHandlerInterfaceType())
                                                            .SelectMany(t => t.GetStreamingRequestAndItemTypes())
                                                            .ToDictionary(t => t.RequestType, t => t.ItemType);

        foreach (var (requestType, itemType) in handlerType.GetStreamingRequestAndItemTypes())
        {
            if (existingStreamingRequestRegistrations.TryGetValue(requestType, out var existingItemType) && itemType != existingItemType)
            {
                throw new InvalidOperationException($"client for streaming request type '{requestType.Name}' is already registered with item type '{existingItemType.Name}', but tried to add client with different item type '{itemType.Name}'");
            }

            var genericAddClientMethod = addClientMethod.MakeGenericMethod(handlerType, requestType, itemType);

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

    private static void AddClient<THandler, TRequest, TItem>(this IServiceCollection services,
                                                             StreamingRequestTransportClientFactory transportClientFactory,
                                                             Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler
        where TRequest : class
    {
        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            _ = services.Replace(ServiceDescriptor.Transient<IStreamingRequestHandler<TRequest, TItem>>(CreateProxy));
        }

        StreamingRequestHandlerProxy<TRequest, TItem> CreateProxy(IServiceProvider serviceProvider)
        {
            var requestMiddlewareRegistry = serviceProvider.GetRequiredService<StreamingRequestMiddlewareRegistry>();
            return new(serviceProvider, transportClientFactory, configurePipeline, requestMiddlewareRegistry);
        }

        void RegisterCustomInterface()
        {
            if (GetCustomStreamingRequestHandlerInterfaceType() is { } customInterfaceType)
            {
                var dynamicType = DynamicType.Create(customInterfaceType, typeof(IStreamingRequestHandler<TRequest, TItem>));
                services.TryAddTransient(customInterfaceType, dynamicType);
            }
        }

        static Type? GetCustomStreamingRequestHandlerInterfaceType()
        {
            var interfaces = typeof(THandler).GetInterfaces()
                                             .Concat(new[] { typeof(THandler) })
                                             .Where(i => i.IsCustomStreamingRequestHandlerInterfaceType<TRequest, TItem>())
                                             .ToList();

            if (interfaces.Count < 1)
            {
                return null;
            }

            if (interfaces.Count > 1)
            {
                throw new InvalidOperationException($"streaming request handler type '{typeof(THandler).Name}' implements more than one custom interface for streaming request '{typeof(TRequest).Name}'");
            }

            var customHandlerInterface = interfaces.Single();

            if (customHandlerInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException($"streaming request handler type '{typeof(THandler).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom streaming request handler interface types are not allowed to have any additional methods beside the '{nameof(IStreamingRequestHandler<object, object>.ExecuteRequest)}' inherited from '{typeof(IStreamingRequestHandler<,>).Name}'");
            }

            return customHandlerInterface;
        }
    }
}
