using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Conqueror.Streaming.Transport.Http.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
public sealed class StreamingHttpClientRegistrationTests
{
    [Test]
    public async Task GivenCustomWebSocketFactory_WhenResolvingClient_UsesCustomFactory()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactory(expectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
    }

    [Test]
    public async Task GivenCustomWebSocketFactory_WhenResolvingClient_UsesProvidedBaseAddress()
    {
        var expectedBaseAddress = new Uri("http://expected.localhost");
        Uri? seenBaseAddress = null;
        var factory = new ConquerorStreamingWebSocketFactory((uri, _, _) =>
        {
            seenBaseAddress = uri;
            return Task.FromResult<WebSocket>(new TestWebSocket());
        });

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactory(factory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b => b.UseWebSocket(expectedBaseAddress));

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenBaseAddress, Is.Not.Null);
        Assert.That(expectedBaseAddress.IsBaseOf(seenBaseAddress!), Is.True);
    }

    [Test]
    public async Task GivenCustomWebSocketFactoryForSameRequestType_WhenResolvingClient_UsesCustomFactory()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactoryForStream<TestRequest>(expectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
    }

    [Test]
    public async Task GivenGlobalCustomFactoryAndCustomFactoryForSameRequestType_WhenResolvingClient_UsesCustomFactoryForRequestType()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactory(unexpectedFactory).UseWebSocketFactoryForStream<TestRequest>(expectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    // test different order of calling UseWebSocketFactoryForStream and UseWebSocketFactory
    [Test]
    public async Task GivenCustomWebSocketFactoryForSameRequestTypeAndGlobalCustomFactory_WhenResolvingClient_UsesCustomFactoryForRequestType()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactoryForStream<TestRequest>(expectedFactory).UseWebSocketFactory(unexpectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    [Test]
    public async Task GivenGlobalCustomFactoryAndCustomFactoryForDifferentRequestType_WhenResolvingClient_UsesGlobalCustomFactory()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactory(expectedFactory).UseWebSocketFactoryForStream<TestRequest2>(unexpectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    [Test]
    public async Task GivenCustomWebSocketFactoryForDifferentRequestType_WhenResolvingClient_DoesNotUseCustomFactory()
    {
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactoryForStream<TestRequest2>(unexpectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    [Test]
    public async Task GivenCustomWebSocketFactoryForRequestTypeAssembly_WhenResolvingClient_UsesCustomFactory()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactoryForTypesFromAssembly(typeof(TestRequest).Assembly, expectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
    }

    [Test]
    public async Task GivenCustomWebSocketFactoryForDifferentAssembly_WhenResolvingClient_DoesNotUseCustomFactory()
    {
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactoryForTypesFromAssembly(typeof(string).Assembly, unexpectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    [Test]
    public async Task GivenGlobalCustomFactoryAndForRequestTypeAssembly_WhenResolvingClient_UsesCustomFactoryForAssembly()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactory(unexpectedFactory)
                                                                   .UseWebSocketFactoryForTypesFromAssembly(typeof(TestRequest).Assembly, expectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    [Test]
    public async Task GivenCustomWebSocketFactoryForRequestTypeAndForRequestTypeAssembly_WhenResolvingClient_UsesCustomFactoryForRequestType()
    {
        var expectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        var unexpectedFactory = new ConquerorStreamingWebSocketFactory((_, _, _) => Task.FromResult<WebSocket>(new ClientWebSocket()));
        ConquerorStreamingWebSocketFactory? seenFactory = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => o.UseWebSocketFactoryForStream<TestRequest>(expectedFactory)
                                                                   .UseWebSocketFactoryForTypesFromAssembly(typeof(TestRequest).Assembly, unexpectedFactory))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var transportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;

                        seenFactory = transportClient?.Options.SocketFactory;

                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenFactory, Is.SameAs(expectedFactory));
        Assert.That(seenFactory, Is.Not.SameAs(unexpectedFactory));
    }

    [Test]
    public async Task GivenCustomConfiguration_WhenResolvingClient_ConfiguresOptionsWithScopedServiceProvider()
    {
        var seenInstances = new HashSet<ScopingTest>();

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => { _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>()); })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        _ = b.UseWebSocket(new("http://localhost"));
                        return new TestStreamingRequestTransport();
                    });

        _ = services.AddScoped<ScopingTest>();

        await using var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();

        var client1 = scope1.ServiceProvider.GetRequiredService<ITestStreamingRequestHandler>();
        var client2 = scope1.ServiceProvider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client1.ExecuteRequest(new()).Drain();
        _ = await client2.ExecuteRequest(new()).Drain();

        using var scope2 = provider.CreateScope();

        var client3 = scope2.ServiceProvider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client3.ExecuteRequest(new()).Drain();

        Assert.That(seenInstances, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GivenGlobalJsonSerializerOptions_WhenExecutingHandler_UsesGlobalJsonSerializerOptions()
    {
        var expectedOptions = new JsonSerializerOptions();
        JsonSerializerOptions? seenOptions = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;
                        seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenOptions, Is.SameAs(expectedOptions));
    }

    [Test]
    public async Task GivenClientJsonSerializerOptions_WhenExecutingHandler_UsesClientJsonSerializerOptions()
    {
        var expectedOptions = new JsonSerializerOptions();
        JsonSerializerOptions? seenOptions = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices()
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost"), o => o.JsonSerializerOptions = expectedOptions) as HttpStreamingRequestTransportClient;
                        seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenOptions, Is.SameAs(expectedOptions));
    }

    [Test]
    public async Task GivenGlobalAndClientJsonSerializerOptions_WhenExecutingHandler_UsesClientJsonSerializerOptions()
    {
        var globalOptions = new JsonSerializerOptions();
        var expectedOptions = new JsonSerializerOptions();
        JsonSerializerOptions? seenOptions = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => { o.JsonSerializerOptions = globalOptions; })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost"), o => o.JsonSerializerOptions = expectedOptions) as HttpStreamingRequestTransportClient;
                        seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenOptions, Is.SameAs(expectedOptions));
        Assert.That(seenOptions, Is.Not.SameAs(globalOptions));
    }

    [Test]
    public async Task GivenGlobalPathConvention_WhenExecutingHandler_UsesGlobalPathConvention()
    {
        var expectedConvention = new TestHttpStreamPathConvention() as IHttpStreamPathConvention;
        IHttpStreamPathConvention? seenConvention = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => { o.PathConvention = expectedConvention; })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;
                        seenConvention = httpTransportClient?.Options.PathConvention;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenConvention, Is.SameAs(expectedConvention));
    }

    [Test]
    public async Task GivenClientPathConvention_WhenExecutingHandler_UsesClientPathConvention()
    {
        var expectedConvention = new TestHttpStreamPathConvention() as IHttpStreamPathConvention;
        IHttpStreamPathConvention? seenConvention = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices()
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost"), o => o.PathConvention = expectedConvention) as HttpStreamingRequestTransportClient;
                        seenConvention = httpTransportClient?.Options.PathConvention;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenConvention, Is.SameAs(expectedConvention));
    }

    [Test]
    public async Task GivenGlobalAndClientPathConvention_WhenExecutingHandler_UsesClientPathConvention()
    {
        var globalConvention = new TestHttpStreamPathConvention() as IHttpStreamPathConvention;
        var expectedConvention = new TestHttpStreamPathConvention() as IHttpStreamPathConvention;
        IHttpStreamPathConvention? seenConvention = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o => { o.PathConvention = globalConvention; })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost"), o => o.PathConvention = expectedConvention) as HttpStreamingRequestTransportClient;
                        seenConvention = httpTransportClient?.Options.PathConvention;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenConvention, Is.SameAs(expectedConvention));
        Assert.That(seenConvention, Is.Not.SameAs(globalConvention));
    }

    [Test]
    public async Task GivenMultipleOptionsConfigurationsFromAddServices_WhenExecutingHandler_UsesMergedOptions()
    {
        var unexpectedOptions = new JsonSerializerOptions();
        var expectedOptions = new JsonSerializerOptions();
        var expectedConvention = new TestHttpStreamPathConvention() as IHttpStreamPathConvention;
        JsonSerializerOptions? seenOptions = null;
        IHttpStreamPathConvention? seenConvention = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o =>
                    {
                        o.JsonSerializerOptions = unexpectedOptions;
                        o.PathConvention = expectedConvention;
                    })
                    .AddConquerorStreamingHttpClientServices(o => { o.JsonSerializerOptions = expectedOptions; })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;
                        seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                        seenConvention = httpTransportClient?.Options.PathConvention;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenOptions, Is.SameAs(expectedOptions));
        Assert.That(seenOptions, Is.Not.SameAs(unexpectedOptions));
        Assert.That(seenConvention, Is.SameAs(expectedConvention));
    }

    [Test]
    public async Task GivenMultipleOptionsConfigurations_WhenExecutingHandler_UsesMergedOptions()
    {
        var unexpectedOptions = new JsonSerializerOptions();
        var expectedOptions = new JsonSerializerOptions();
        var expectedConvention = new TestHttpStreamPathConvention() as IHttpStreamPathConvention;
        JsonSerializerOptions? seenOptions = null;
        IHttpStreamPathConvention? seenConvention = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o =>
                    {
                        o.JsonSerializerOptions = unexpectedOptions;
                        o.PathConvention = expectedConvention;
                    })
                    .ConfigureConquerorCQSHttpClientOptions(o => { o.JsonSerializerOptions = expectedOptions; })
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b =>
                    {
                        var httpTransportClient = b.UseWebSocket(new("http://localhost")) as HttpStreamingRequestTransportClient;
                        seenOptions = httpTransportClient?.Options.JsonSerializerOptions;
                        seenConvention = httpTransportClient?.Options.PathConvention;
                        return new TestStreamingRequestTransport();
                    });

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = await client.ExecuteRequest(new()).Drain();

        Assert.That(seenOptions, Is.SameAs(expectedOptions));
        Assert.That(seenOptions, Is.Not.SameAs(unexpectedOptions));
        Assert.That(seenConvention, Is.SameAs(expectedConvention));
    }

    [Test]
    public async Task GivenClientConfigurationWithRelativeBaseAddress_WhenExecutingHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices()
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b => b.UseWebSocket(new("/", UriKind.Relative)));

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteRequest(new(), CancellationToken.None).Drain());
    }

    [Test]
    public async Task GivenClientConfigurationWithNullBaseAddress_WhenExecutingHandler_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices()
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b => b.UseWebSocket(null!));

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestStreamingRequestHandler>();

        var thrownException = Assert.ThrowsAsync<ArgumentNullException>(() => client.ExecuteRequest(new(), CancellationToken.None).Drain());

        Assert.That(thrownException?.ParamName, Is.EqualTo("baseAddress"));
    }

    [Test]
    public async Task GivenNonHttpPlainHandlerInterface_WhenExecutingHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices()
                    .AddConquerorStreamingRequestClient<IStreamingRequestHandler<NonHttpTestRequest, TestItem>>(b => b.UseWebSocket(new("http://localhost")));

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IStreamingRequestHandler<NonHttpTestRequest, TestItem>>();

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteRequest(new(), CancellationToken.None).Drain());
    }

    [Test]
    public async Task GivenNonHttpCustomHandlerInterface_WhenExecutingHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices()
                    .AddConquerorStreamingRequestClient<INonHttpTestStreamingRequestHandler>(b => b.UseWebSocket(new("http://localhost")));

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<INonHttpTestStreamingRequestHandler>();

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteRequest(new(), CancellationToken.None).Drain());
    }

    [HttpStream]
    public sealed record TestRequest
    {
        public int Payload { get; init; }
    }

    public sealed record TestItem
    {
        public int Payload { get; init; }
    }

    [HttpStream]
    public sealed record TestRequest2;

    public sealed record NonHttpTestRequest;

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestRequest, TestItem>;

    public interface INonHttpTestStreamingRequestHandler : IStreamingRequestHandler<NonHttpTestRequest, TestItem>;

    private sealed class TestStreamingRequestTransport : IStreamingRequestTransportClient
    {
        public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                             IServiceProvider serviceProvider,
                                                                             [EnumeratorCancellation] CancellationToken cancellationToken)
            where TRequest : class
        {
            await Task.Yield();
            yield return (TItem)(object)new TestItem();
        }
    }

    private sealed class TestHttpStreamPathConvention : IHttpStreamPathConvention
    {
        public string? GetStreamPath(Type requestType, HttpStreamAttribute attribute)
        {
            return null;
        }
    }

    private sealed class ScopingTest;

    private sealed class TestWebSocket : WebSocket
    {
        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
        public override string? SubProtocol => null;

        public override void Abort()
        {
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

        public override void Dispose()
        {
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
            Task.FromResult(new WebSocketReceiveResult(1, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, string.Empty));

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
