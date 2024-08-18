using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror.Common;

namespace Conqueror.Streaming;

// TODO: improve performance by caching creation functions via compiled expressions
internal sealed class StreamingRequestClientFactory
{
    private readonly StreamingRequestMiddlewareRegistry requestMiddlewareRegistry;

    public StreamingRequestClientFactory(StreamingRequestMiddlewareRegistry requestMiddlewareRegistry)
    {
        this.requestMiddlewareRegistry = requestMiddlewareRegistry;
    }

    public THandler CreateStreamingRequestClient<THandler>(IServiceProvider serviceProvider,
                                                           Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>> transportClientFactory,
                                                           Action<IStreamingRequestPipelineBuilder>? configurePipeline)
        where THandler : class, IStreamingRequestHandler
    {
        typeof(THandler).ValidateNoInvalidStreamingRequestHandlerInterface();

        if (!typeof(THandler).IsInterface)
        {
            throw new ArgumentException($"can only create streaming request client for streaming request handler interfaces, got concrete type '{typeof(THandler).Name}'");
        }

        var requestAndItemTypes = typeof(THandler).GetStreamingRequestAndItemTypes();

        switch (requestAndItemTypes.Count)
        {
            case < 1:
                throw new ArgumentException($"type {typeof(THandler).Name} does not implement any streaming request handler interface");

            case > 1:
                throw new ArgumentException($"type {typeof(THandler).Name} implements multiple streaming request handler interfaces");
        }

        var (requestType, itemType) = requestAndItemTypes.First();

        var creationMethod = typeof(StreamingRequestClientFactory).GetMethod(nameof(CreateClientInternal), BindingFlags.NonPublic | BindingFlags.Static);

        if (creationMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(CreateClientInternal)}'");
        }

        var genericCreationMethod = creationMethod.MakeGenericMethod(typeof(THandler), requestType, itemType);

        try
        {
            var result = genericCreationMethod.Invoke(null, [serviceProvider, transportClientFactory, configurePipeline, requestMiddlewareRegistry]);

            if (result is not THandler handler)
            {
                throw new InvalidOperationException($"failed to create streaming request client for handler type '{typeof(THandler).Name}'");
            }

            return handler;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static THandler CreateClientInternal<THandler, TRequest, TItem>(IServiceProvider serviceProvider,
                                                                            Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>> transportClientFactory,
                                                                            Action<IStreamingRequestPipelineBuilder>? configurePipeline,
                                                                            StreamingRequestMiddlewareRegistry requestMiddlewareRegistry)
        where THandler : class, IStreamingRequestHandler
        where TRequest : class
    {
        var proxy = new StreamingRequestHandlerProxy<TRequest, TItem>(serviceProvider, new(transportClientFactory), configurePipeline, requestMiddlewareRegistry);

        if (typeof(THandler) == typeof(IStreamingRequestHandler<TRequest, TItem>))
        {
            return (THandler)(object)proxy;
        }

        if (typeof(THandler).IsAssignableTo(typeof(IStreamingRequestHandler<TRequest, TItem>)))
        {
            var dynamicType = DynamicType.Create(typeof(THandler), typeof(IStreamingRequestHandler<TRequest, TItem>));
            return (THandler)Activator.CreateInstance(dynamicType, proxy)!;
        }

        throw new InvalidOperationException($"streaming request handler type '{typeof(THandler).Name}' does not implement a known streaming request handler interface");
    }
}
