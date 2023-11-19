using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Transport.WebSockets.Client;

public sealed class ConquerorEventingWebSocketsTransportClientGlobalOptions
{
    internal ConquerorEventingWebSocketsTransportClientGlobalOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public string? EndpointPath { get; set; }

    internal Func<Uri, Task<WebSocket>>? GlobalWebSocketFactory { get; private set; }

    internal Uri? GlobalBaseAddress { get; private set; }

    internal Dictionary<Type, Uri>? BaseAddressesForEventTypes { get; private set; }

    internal Dictionary<Assembly, Uri>? BaseAddressesForAssembly { get; private set; }

    public ConquerorEventingWebSocketsTransportClientGlobalOptions UseBaseAddressForEventType<T>(Uri baseAddress)
        where T : notnull
    {
        BaseAddressesForEventTypes ??= new();

        BaseAddressesForEventTypes[typeof(T)] = baseAddress;

        return this;
    }

    public ConquerorEventingWebSocketsTransportClientGlobalOptions UseBaseAddressForEventTypesFromAssembly(Assembly assembly, Uri baseAddress)
    {
        BaseAddressesForAssembly ??= new();

        BaseAddressesForAssembly[assembly] = baseAddress;

        return this;
    }

    public ConquerorEventingWebSocketsTransportClientGlobalOptions UseBaseAddress(Uri baseAddress)
    {
        GlobalBaseAddress = baseAddress;
        return this;
    }

    public ConquerorEventingWebSocketsTransportClientGlobalOptions UseWebSocketFactory(Func<Uri, Task<WebSocket>> webSocketFactory)
    {
        GlobalWebSocketFactory = webSocketFactory;
        return this;
    }
}
