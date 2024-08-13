using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Transport.Http.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
public sealed class StreamingHttpClientRegistrationTests
{
    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<IStreamingHandler<TestRequest, TestItem>>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolvePlainClient()
    {
        using var provider = RegisterClient<ITestStreamingHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>());
    }

    [Test]
    public void GivenRegisteredCustomClient_CanResolveCustomClient()
    {
        using var provider = RegisterClient<ITestStreamingHandler>();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestStreamingHandler>());
    }

    [Test]
    public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestStreamingHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStreamingHandler<NonHttpTestRequest, TestItem>>());
    }

    [Test]
    public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
    {
        using var provider = RegisterClient<ITestStreamingHandler>();
        _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<INonHttpTestStreamingHandler>());
    }

    [Test]
    public void GivenNonHttpPlainStreamingHandlerType_RegistrationThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => RegisterClient<IStreamingHandler<NonHttpTestRequest, TestItem>>());
    }

    [Test]
    public void GivenNonHttpCustomStreamingHandlerType_RegistrationThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => RegisterClient<INonHttpTestStreamingHandler>());
    }

    [Test]
    public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
    {
        var provider = new ServiceCollection().AddConquerorStreamingHttpClient<IStreamingHandler<TestRequest, TestItem>>(_ => new("http://localhost"))
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IStreamingHandler<TestRequest, TestItem>>());
    }

    [Test]
    public void GivenAlreadyRegisteredHandlerWhenRegistering_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices().AddConquerorStreamingHttpClient<ITestStreamingHandler>(_ => new("http://localhost"));

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorStreamingHttpClientServices()
                                                                   .AddConquerorStreamingHttpClient<ITestStreamingHandler>(_ => new("http://localhost")));
    }

    [Test]
    public async Task GivenCustomWebSocketFactory_WhenResolvingHandlerRegisteredWithBaseAddressFactory_CallsCustomWebSocketFactory()
    {
        var expectedAddress = new Uri("http://localhost/api/streams/test?payload=0");
        Uri? seenAddress = null;

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o =>
                    {
                        o.WebSocketFactory = async (address, _) =>
                        {
                            await Task.Yield();
                            seenAddress = address;
                            return new TestWebSocket();
                        };
                    })
                    .AddConquerorStreamingHttpClient<ITestStreamingHandler>(_ => expectedAddress);

        await using var provider = services.BuildServiceProvider();

        _ = await provider.GetRequiredService<ITestStreamingHandler>()
                          .ExecuteRequest(new(), CancellationToken.None)
                          .GetAsyncEnumerator()
                          .MoveNextAsync();

        Assert.That(seenAddress, Is.EqualTo(expectedAddress));
    }

    [Test]
    public async Task GivenCustomWebSocketFactory_CallsFactoryWithScopedServiceProvider()
    {
        var seenInstances = new HashSet<ScopingTest>();

        var services = new ServiceCollection();
        _ = services.AddConquerorStreamingHttpClientServices(o =>
                    {
                        o.WebSocketFactory = async (address, ct) =>
                        {
                            await Task.Yield();
                            _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>());
                            return new TestWebSocket();
                        };
                    })
                    .AddConquerorStreamingHttpClient<ITestStreamingHandler>(_ => new("http://localhost"));

        _ = services.AddScoped<ScopingTest>();

        await using var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();

        _ = await scope1.ServiceProvider
                        .GetRequiredService<ITestStreamingHandler>()
                        .ExecuteRequest(new(), CancellationToken.None)
                        .GetAsyncEnumerator()
                        .MoveNextAsync();

        _ = await scope1.ServiceProvider
                        .GetRequiredService<ITestStreamingHandler>()
                        .ExecuteRequest(new(), CancellationToken.None)
                        .GetAsyncEnumerator()
                        .MoveNextAsync();

        using var scope2 = provider.CreateScope();

        _ = await scope2.ServiceProvider
                        .GetRequiredService<ITestStreamingHandler>()
                        .ExecuteRequest(new(), CancellationToken.None)
                        .GetAsyncEnumerator()
                        .MoveNextAsync();

        Assert.That(seenInstances, Has.Count.EqualTo(2));
    }

    private static ServiceProvider RegisterClient<TStreamingHandler>()
        where TStreamingHandler : class, IStreamingHandler
    {
        return new ServiceCollection().AddConquerorStreamingHttpClientServices()
                                      .AddConquerorStreamingHttpClient<TStreamingHandler>(_ => new("http://localhost"))
                                      .BuildServiceProvider();
    }

    private sealed class ScopingTest
    {
    }

    [HttpStreamingRequest]
    public sealed record TestRequest
    {
        public int Payload { get; init; }
    }

    public sealed record TestItem
    {
        public int Payload { get; init; }
    }

    private sealed class TestStreamingHandler : ITestStreamingHandler
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new() { Payload = request.Payload + 1 };
        }
    }

    public interface ITestStreamingHandler : IStreamingHandler<TestRequest, TestItem>
    {
    }

    public sealed record NonHttpTestRequest
    {
        public int Payload { get; init; }
    }

    private sealed class NonHttpTestStreamingHandler : INonHttpTestStreamingHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(NonHttpTestRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    public interface INonHttpTestStreamingHandler : IStreamingHandler<NonHttpTestRequest, TestItem>
    {
    }

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
