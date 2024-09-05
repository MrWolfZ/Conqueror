using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror.Common;

namespace Conqueror.Streaming;

// TODO: improve performance by caching creation functions via compiled expressions
internal sealed class StreamProducerClientFactory
{
    private readonly StreamProducerMiddlewareRegistry producerMiddlewareRegistry;

    public StreamProducerClientFactory(StreamProducerMiddlewareRegistry producerMiddlewareRegistry)
    {
        this.producerMiddlewareRegistry = producerMiddlewareRegistry;
    }

    public TProducer CreateStreamProducerClient<TProducer>(IServiceProvider serviceProvider,
                                                           Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory,
                                                           Action<IStreamProducerPipelineBuilder>? configurePipeline)
        where TProducer : class, IStreamProducer
    {
        typeof(TProducer).ValidateNoInvalidStreamProducerInterface();

        if (!typeof(TProducer).IsInterface)
        {
            throw new ArgumentException($"can only create stream producer client for stream producer interfaces, got concrete type '{typeof(TProducer).Name}'");
        }

        var requestAndItemTypes = typeof(TProducer).GetStreamProducerRequestAndItemTypes();

        switch (requestAndItemTypes.Count)
        {
            case < 1:
                throw new ArgumentException($"type {typeof(TProducer).Name} does not implement any stream producer interface");

            case > 1:
                throw new ArgumentException($"type {typeof(TProducer).Name} implements multiple stream producer interfaces");
        }

        var (requestType, itemType) = requestAndItemTypes.First();

        var creationMethod = typeof(StreamProducerClientFactory).GetMethod(nameof(CreateClientInternal), BindingFlags.NonPublic | BindingFlags.Static);

        if (creationMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(CreateClientInternal)}'");
        }

        var genericCreationMethod = creationMethod.MakeGenericMethod(typeof(TProducer), requestType, itemType);

        try
        {
            var result = genericCreationMethod.Invoke(null, [serviceProvider, transportClientFactory, configurePipeline, producerMiddlewareRegistry]);

            if (result is not TProducer producer)
            {
                throw new InvalidOperationException($"failed to create stream producer client for producer type '{typeof(TProducer).Name}'");
            }

            return producer;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable code that is necessary so that the compiler knows the catch throws
        }
    }

    private static TProducer CreateClientInternal<TProducer, TRequest, TItem>(IServiceProvider serviceProvider,
                                                                              Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory,
                                                                              Action<IStreamProducerPipelineBuilder>? configurePipeline,
                                                                              StreamProducerMiddlewareRegistry producerMiddlewareRegistry)
        where TProducer : class, IStreamProducer
        where TRequest : class
    {
        var proxy = new StreamProducerProxy<TRequest, TItem>(serviceProvider, new(transportClientFactory), configurePipeline, producerMiddlewareRegistry);

        if (typeof(TProducer) == typeof(IStreamProducer<TRequest, TItem>))
        {
            return (TProducer)(object)proxy;
        }

        if (typeof(TProducer).IsAssignableTo(typeof(IStreamProducer<TRequest, TItem>)))
        {
            var proxyType = ProxyTypeGenerator.Create(typeof(TProducer), typeof(IStreamProducer<TRequest, TItem>), typeof(StreamProducerGeneratedProxyBase<TRequest, TItem>));
            return (TProducer)Activator.CreateInstance(proxyType, proxy)!;
        }

        throw new InvalidOperationException($"stream producer type '{typeof(TProducer).Name}' does not implement a known stream producer interface");
    }
}
