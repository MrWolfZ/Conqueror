using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class ConfigurationProvider
{
    private readonly IReadOnlyCollection<Action<ConquerorStreamingHttpClientGlobalOptions>> configureGlobalOptions;
    private readonly ConcurrentDictionary<Type, bool> httpValidityByRequestType = new();

    public ConfigurationProvider(IEnumerable<Action<ConquerorStreamingHttpClientGlobalOptions>> configureGlobalOptions)
    {
        this.configureGlobalOptions = configureGlobalOptions.ToList();
    }

    public ResolvedHttpClientOptions GetOptions(IServiceProvider provider, HttpClientRegistration registration)
    {
        ValidateRequestType(registration);

        var queryOptions = new HttpStreamClientOptions(provider);
        registration.StreamConfigurationAction?.Invoke(queryOptions);

        var globalOptions = new ConquerorStreamingHttpClientGlobalOptions(provider);

        foreach (var configure in configureGlobalOptions)
        {
            configure(globalOptions);
        }

        var socketFactory = CreateWebSocket(registration, globalOptions);

        var baseAddress = registration.BaseAddress;

        if (!baseAddress.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"configuration error while creating options for Conqueror HTTP transport client: base address must be an absolute URI, but got '{baseAddress}'");
        }

        var jsonSerializerOptions = queryOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;
        var queryPathConvention = queryOptions.PathConvention ?? globalOptions.PathConvention;
        var headers = queryOptions.OptionalHeaders;

        return new(socketFactory, baseAddress, jsonSerializerOptions, queryPathConvention, headers);
    }

    private ConquerorStreamingWebSocketFactory CreateWebSocket(HttpClientRegistration registration, ConquerorStreamingHttpClientGlobalOptions globalOptions)
    {
        if (registration.RequestType is not null && (globalOptions.StreamWebSocketFactories?.TryGetValue(registration.RequestType, out var queryClientFactory) ?? false))
        {
            return queryClientFactory;
        }

        var requestType = registration.RequestType;

        if (requestType is not null && (globalOptions.AssemblyWebSocketFactories?.TryGetValue(requestType.Assembly, out var assemblyClientFactory) ?? false))
        {
            return assemblyClientFactory;
        }

        return globalOptions.GlobalWebSocketFactory ?? DefaultWebSocketFactory;
    }

    private async Task<WebSocket> DefaultWebSocketFactory(Uri uri, HttpRequestHeaders headers, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var socket = new ClientWebSocket();

        foreach (var (key, values) in headers)
        {
            foreach (var value in values)
            {
                socket.Options.SetRequestHeader(key, value);
            }
        }

        await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

        return socket;
    }

    private void ValidateRequestType(HttpClientRegistration registration)
    {
        if (registration.RequestType is { } requestType)
        {
            var isHttpQuery = httpValidityByRequestType.GetOrAdd(requestType, t => t.GetCustomAttribute<HttpStreamAttribute>() != null);

            if (!isHttpQuery)
            {
                throw new InvalidOperationException($"streaming request type '{requestType.Name}' is marked as an HTTP stream; did you forget to add the '[{nameof(HttpStreamAttribute)}]'?");
            }
        }
    }
}
