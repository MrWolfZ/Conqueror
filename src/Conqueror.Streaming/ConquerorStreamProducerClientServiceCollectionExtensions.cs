using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamProducerClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamProducerClient<TProducer>(this IServiceCollection services,
                                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer
    {
        return services.AddConquerorStreamProducerClient(typeof(TProducer), transportClientFactory, configurePipeline);
    }

    public static IServiceCollection AddConquerorStreamProducerClient<TProducer>(this IServiceCollection services,
                                                                                 Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory,
                                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer
    {
        return services.AddConquerorStreamProducerClient(typeof(TProducer), transportClientFactory, configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamProducerClient(this IServiceCollection services,
                                                                        Type producerType,
                                                                        IStreamProducerTransportClient transportClient,
                                                                        Action<IStreamProducerPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorStreamProducerClient(producerType, new StreamProducerTransportClientFactory(transportClient), configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamProducerClient(this IServiceCollection services,
                                                                        Type producerType,
                                                                        Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                        Action<IStreamProducerPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorStreamProducerClient(producerType, new StreamProducerTransportClientFactory(transportClientFactory), configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamProducerClient(this IServiceCollection services,
                                                                        Type producerType,
                                                                        Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory,
                                                                        Action<IStreamProducerPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorStreamProducerClient(producerType, new StreamProducerTransportClientFactory(transportClientFactory), configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamProducerClient(this IServiceCollection services,
                                                                        Type producerType,
                                                                        StreamProducerTransportClientFactory transportClientFactory,
                                                                        Action<IStreamProducerPipelineBuilder>? configurePipeline)
    {
        producerType.ValidateNoInvalidStreamProducerInterface();

        services.AddConquerorStreaming();

        var addClientMethod = typeof(ConquerorStreamProducerClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

        if (addClientMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
        }

        var existingStreamProducerRegistrations = services.Select(d => d.ServiceType)
                                                          .Where(t => t.IsStreamProducerInterfaceType())
                                                          .SelectMany(t => t.GetStreamProducerRequestAndItemTypes())
                                                          .ToDictionary(t => t.RequestType, t => t.ItemType);

        foreach (var (requestType, itemType) in producerType.GetStreamProducerRequestAndItemTypes())
        {
            if (existingStreamProducerRegistrations.TryGetValue(requestType, out var existingItemType) && itemType != existingItemType)
            {
                throw new InvalidOperationException($"client for streaming request type '{requestType.Name}' is already registered with item type '{existingItemType.Name}', but tried to add client with different item type '{itemType.Name}'");
            }

            var genericAddClientMethod = addClientMethod.MakeGenericMethod(producerType, requestType, itemType);

            try
            {
                _ = genericAddClientMethod.Invoke(null, [services, transportClientFactory, configurePipeline]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddClient<TProducer, TRequest, TItem>(this IServiceCollection services,
                                                              StreamProducerTransportClientFactory transportClientFactory,
                                                              Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer
        where TRequest : class
    {
        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            _ = services.Replace(ServiceDescriptor.Transient<IStreamProducer<TRequest, TItem>>(CreateProxy));
        }

        StreamProducerProxy<TRequest, TItem> CreateProxy(IServiceProvider serviceProvider)
        {
            var producerMiddlewareRegistry = serviceProvider.GetRequiredService<StreamProducerMiddlewareRegistry>();
            return new(serviceProvider, transportClientFactory, configurePipeline, producerMiddlewareRegistry);
        }

        void RegisterCustomInterface()
        {
            if (GetCustomStreamProducerInterfaceType() is { } customInterfaceType)
            {
                var proxyType = ProxyTypeGenerator.Create(customInterfaceType, typeof(IStreamProducer<TRequest, TItem>), typeof(StreamProducerGeneratedProxyBase<TRequest, TItem>));
                services.TryAddTransient(customInterfaceType, proxyType);
            }
        }

        static Type? GetCustomStreamProducerInterfaceType()
        {
            var interfaces = typeof(TProducer).GetInterfaces()
                                              .Concat([typeof(TProducer)])
                                              .Where(i => i.IsCustomStreamProducerInterfaceType<TRequest, TItem>())
                                              .ToList();

            if (interfaces.Count < 1)
            {
                return null;
            }

            if (interfaces.Count > 1)
            {
                throw new InvalidOperationException($"stream producer type '{typeof(TProducer).Name}' implements more than one custom interface for streaming request '{typeof(TRequest).Name}'");
            }

            var customProducerInterface = interfaces.Single();

            if (customProducerInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException($"stream producer type '{typeof(TProducer).Name}' implements custom interface '{customProducerInterface.Name}' that has extra methods; custom stream producer interface types are not allowed to have any additional methods beside the '{nameof(IStreamProducer<object, object>.ExecuteRequest)}' inherited from '{typeof(IStreamProducer<,>).Name}'");
            }

            return customProducerInterface;
        }
    }
}
