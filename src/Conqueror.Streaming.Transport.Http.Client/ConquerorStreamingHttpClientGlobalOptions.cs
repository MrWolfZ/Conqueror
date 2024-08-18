using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Client;

public delegate Task<WebSocket> ConquerorStreamingWebSocketFactory(Uri uri, HttpRequestHeaders headers, CancellationToken cancellationToken);

public sealed class ConquerorStreamingHttpClientGlobalOptions
{
    internal ConquerorStreamingHttpClientGlobalOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public IHttpStreamPathConvention? PathConvention { get; set; }

    public ConquerorStreamingWebSocketFactory? GlobalWebSocketFactory { get; private set; }

    internal Dictionary<Type, ConquerorStreamingWebSocketFactory>? StreamWebSocketFactories { get; private set; }

    internal Dictionary<Assembly, ConquerorStreamingWebSocketFactory>? AssemblyWebSocketFactories { get; private set; }

    public ConquerorStreamingHttpClientGlobalOptions UseWebSocketFactoryForStream<T>(ConquerorStreamingWebSocketFactory factory)
        where T : notnull
    {
        StreamWebSocketFactories ??= new();

        StreamWebSocketFactories[typeof(T)] = factory;

        return this;
    }

    public ConquerorStreamingHttpClientGlobalOptions UseWebSocketFactoryForTypesFromAssembly(Assembly assembly, ConquerorStreamingWebSocketFactory factory)
    {
        AssemblyWebSocketFactories ??= new();

        AssemblyWebSocketFactories[assembly] = factory;

        return this;
    }

    public ConquerorStreamingHttpClientGlobalOptions UseWebSocketFactory(ConquerorStreamingWebSocketFactory factory)
    {
        GlobalWebSocketFactory = factory;
        return this;
    }
}
