using System;
using System.Linq;
using System.Reflection;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class HttpClientFactory
{
    private readonly ConfigurationProvider configurationProvider;

    public HttpClientFactory(ConfigurationProvider configurationProvider)
    {
        this.configurationProvider = configurationProvider;
    }

    public TStreamingHandler CreateStreamingHttpClient<TStreamingHandler>(IServiceProvider serviceProvider,
                                                                          Func<IServiceProvider, Uri> baseAddressFactory,
                                                                          Action<ConquerorStreamingHttpClientOptions>? configureOptions = null)
        where TStreamingHandler : class, IStreamingHandler
    {
        var (requestType, itemType) = typeof(TStreamingHandler).GetStreamingRequestAndItemTypes().Single();
        requestType.AssertRequestIsHttpStream();

        var registration = new HttpClientRegistration(typeof(TStreamingHandler), baseAddressFactory)
        {
            ConfigurationAction = configureOptions,
        };

        var method = typeof(HttpClientFactory).GetMethod(nameof(CreateTypedQueryClientGeneric), BindingFlags.Instance | BindingFlags.NonPublic);
        var typedMethod = method?.MakeGenericMethod(typeof(TStreamingHandler), requestType, itemType);
        var result = typedMethod?.Invoke(this, new object?[] { serviceProvider, registration });
        return result as TStreamingHandler ?? throw new InvalidOperationException($"could not created typed streaming client for handler type {typeof(TStreamingHandler).Name}");
    }

    private object? CreateTypedQueryClientGeneric<TStreamingHandler, TRequest, TItem>(IServiceProvider serviceProvider, HttpClientRegistration registration)
        where TStreamingHandler : class, IStreamingHandler
        where TRequest : class
    {
        var handler = new HttpStreamingHandler<TRequest, TItem>(configurationProvider.GetOptions(serviceProvider, registration),
                                                                serviceProvider.GetService<IConquerorContextAccessor>());

        if (!typeof(TStreamingHandler).IsCustomStreamingHandlerInterfaceType())
        {
            return handler;
        }

        var dynamicType = DynamicType.Create(typeof(TStreamingHandler), typeof(IStreamingHandler<TRequest, TItem>));
        return Activator.CreateInstance(dynamicType, handler);
    }
}
