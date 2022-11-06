using System.Net.WebSockets;
using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client.Tests
{
    [TestFixture]
    public sealed class InteractiveStreamingHttpClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<IInteractiveStreamingHandler<TestRequest, TestItem>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestStreamingHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>());
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
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IInteractiveStreamingHandler<NonHttpTestRequest, TestItem>>());
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
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<IInteractiveStreamingHandler<NonHttpTestRequest, TestItem>>());
        }

        [Test]
        public void GivenNonHttpCustomStreamingHandlerType_RegistrationThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => RegisterClient<INonHttpTestStreamingHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorInteractiveStreamingHttpClient<IInteractiveStreamingHandler<TestRequest, TestItem>>(_ => new("http://localhost"))
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IInteractiveStreamingHandler<TestRequest, TestItem>>());
        }

        [Test]
        public void GivenAlreadyRegisteredHandlerWhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorInteractiveStreamingHttpClientServices().AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandler>(_ => new("http://localhost"));

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorInteractiveStreamingHttpClientServices()
                                                                       .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandler>(_ => new("http://localhost")));
        }

        [Test]
        public async Task GivenCustomWebSocketFactory_WhenResolvingHandlerRegisteredWithBaseAddressFactory_CallsCustomWebSocketFactory()
        {
            var expectedAddress = new Uri("http://localhost/api/streams/interactive/test?payload=0");
            Uri? seenAddress = null;

            var services = new ServiceCollection();
            _ = services.AddConquerorInteractiveStreamingHttpClientServices(o =>
                        {
                            o.WebSocketFactory = async (address, _) =>
                            {
                                await Task.Yield();
                                seenAddress = address;
                                return new TestWebSocket();
                            };
                        })
                        .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandler>(_ => expectedAddress);

            await using var provider = services.BuildServiceProvider();

            _ = await provider.GetRequiredService<ITestStreamingHandler>()
                              .ExecuteRequest(new(), CancellationToken.None)
                              .GetAsyncEnumerator()
                              .MoveNextAsync();

            Assert.AreEqual(expectedAddress, seenAddress);
        }

        [Test]
        public async Task GivenCustomWebSocketFactory_CallsFactoryWithScopedServiceProvider()
        {
            var seenInstances = new HashSet<ScopingTest>();

            var services = new ServiceCollection();
            _ = services.AddConquerorInteractiveStreamingHttpClientServices(o =>
                        {
                            o.WebSocketFactory = async (address, ct) =>
                            {
                                await Task.Yield();
                                _ = seenInstances.Add(o.ServiceProvider.GetRequiredService<ScopingTest>());
                                return new TestWebSocket();
                            };
                        })
                        .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandler>(_ => new("http://localhost"));

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

            Assert.AreEqual(2, seenInstances.Count);
        }

        private static ServiceProvider RegisterClient<TStreamingHandler>()
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            return new ServiceCollection().AddConquerorInteractiveStreamingHttpClientServices()
                                          .AddConquerorInteractiveStreamingHttpClient<TStreamingHandler>(_ => new("http://localhost"))
                                          .BuildServiceProvider();
        }

        private sealed class ScopingTest
        {
        }

// request, response, and interface types must be public for dynamic type generation to work
#pragma warning disable CA1034

        [HttpInteractiveStreamingRequest]
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

        public interface ITestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
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

        public interface INonHttpTestStreamingHandler : IInteractiveStreamingHandler<NonHttpTestRequest, TestItem>
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
}
