using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
    public sealed class InteractiveStreamingHttpClientTests : TestBase
    {
        [Test]
        public async Task GivenStreamingRequest_StreamsItems()
        {
            var handler = ResolveOnClient<ITestStreamingHandler>();

            var result = await handler.ExecuteRequest(new(10), TestTimeoutToken).Drain(TestTimeoutToken);

            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(new[] { 11, 12, 13 }, result.Select(i => i.Payload));
        }

        [Test]
        public async Task GivenStreamingRequestWithoutPayload_StreamsItems()
        {
            var handler = ResolveOnClient<ITestStreamingHandlerWithoutPayload>();

            var result = await handler.ExecuteRequest(new(), TestTimeoutToken).Drain(TestTimeoutToken);

            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Select(i => i.Payload));
        }

        [Test]
        public async Task GivenStreamingRequestWithCustomSerializedItemType_StreamsItems()
        {
            var handler = ResolveOnClient<ITestStreamingHandlerWithCustomSerializedItemType>();

            var result = await handler.ExecuteRequest(new(10), TestTimeoutToken).Drain(TestTimeoutToken);

            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(new[] { 11, 12, 13 }, result.Select(i => i.Payload.Payload));
        }

        [Test]
        public async Task GivenStreamingRequestWithCollectionPayload_StreamsItems()
        {
            var handler = ResolveOnClient<ITestStreamingHandlerWithCollectionPayload>();

            var result = await handler.ExecuteRequest(new(new() { 10, 11 }), TestTimeoutToken).Drain(TestTimeoutToken);

            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(new[] { 22, 23, 24 }, result.Select(i => i.Payload));
        }

        [Test]
        public async Task GivenStreamingRequest_WhenErrorOccursOnServer_HttpInteractiveStreamingExceptionIsThrown()
        {
            var handler = ResolveOnClient<ITestStreamingHandlerWithError>();

            using var cts = new CancellationTokenSource();

            var stream = handler.ExecuteRequest(new(), TestTimeoutToken);

            var enumerator = stream.GetAsyncEnumerator(cts.Token);

            Assert.IsTrue(await enumerator.MoveNextAsync());

            _ = Assert.ThrowsAsync<HttpInteractiveStreamingException>(() => enumerator.MoveNextAsync().AsTask());
        }

        [Test]
        public async Task GivenStreamingRequest_WhenCancellingRead_CancellationIsPropagatedToServer()
        {
            var handler = ResolveOnClient<ITestStreamingHandler>();

            using var cts = new CancellationTokenSource();

            var stream = handler.ExecuteRequest(new(10), TestTimeoutToken);

            var enumerator = stream.GetAsyncEnumerator(cts.Token);

            _ = await enumerator.MoveNextAsync();

            cts.Cancel();

            Resolve<TestObservations>().CancelledRequests.ShouldReceiveItem(new(10));
        }

        [Test]
        public async Task GivenStreamingRequest_WhenCancellingInParallel_DoesNotThrowException()
        {
            // empirically 100 attempts seem to be the sweet spot for triggering race conditions
            for (var i = 0; i < 100; i += 1)
            {
                var handler = ResolveOnClient<ITestStreamingHandler>();

                using var cts = new CancellationTokenSource();

                var stream = handler.ExecuteRequest(new(10), TestTimeoutToken);

                var enumerator = stream.GetAsyncEnumerator(cts.Token);

                _ = await enumerator.MoveNextAsync();
                _ = await enumerator.MoveNextAsync();
                _ = await enumerator.MoveNextAsync();

                // this move beyond the end of the sequence should cause the server to close the
                // connection, and in parallel we will try to close the connection on the client
                // to try and trigger a race condition
                var lastMoveTask = enumerator.MoveNextAsync();

                Assert.DoesNotThrow(() => cts.Cancel());

                try
                {
                    _ = await lastMoveTask;
                }
                catch (OperationCanceledException)
                {
                    // ignore cancellation
                }
            }
        }

        protected override void ConfigureServerServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorInteractiveStreaming();
            _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestItemJsonConverterFactory()); });

            _ = services.AddTransient<TestStreamingHandler>()
                        .AddTransient<TestStreamingHandlerWithoutPayload>()
                        .AddTransient<TestStreamingHandlerWithCollectionPayload>()
                        .AddTransient<TestStreamingHandlerWithCustomSerializedItemType>()
                        .AddTransient<TestStreamingHandlerWithError>()
                        .AddTransient<NonHttpTestStreamingHandler>()
                        .AddSingleton<TestObservations>();

            _ = services.AddConquerorInteractiveStreaming().FinalizeConquerorRegistrations();
        }

        protected override void ConfigureClientServices(IServiceCollection services)
        {
            _ = services.AddConquerorInteractiveStreamingHttpClientServices(o =>
            {
                o.WebSocketFactory = (uri, _) => ConnectToWebSocket(uri.AbsolutePath, uri.Query);

                o.JsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                };
            });

            _ = services.AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandler>(_ => new("http://example"))
                        .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandlerWithoutPayload>(_ => new("http://example"))
                        .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandlerWithCollectionPayload>(_ => new("http://example"))
                        .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandlerWithCustomSerializedItemType>(_ => new("http://example"), o => o.JsonSerializerOptions = new()
                        {
                            Converters = { new TestItemJsonConverterFactory() },
                            PropertyNameCaseInsensitive = true,
                        })
                        .AddConquerorInteractiveStreamingHttpClient<ITestStreamingHandlerWithError>(_ => new("http://example"));
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        [HttpInteractiveStreamingRequest]
        public sealed record TestRequest(int Payload);

        public sealed record TestItem(int Payload);

        private sealed class TestStreamingHandler : ITestStreamingHandler
        {
            private readonly TestObservations testObservations;

            public TestStreamingHandler(TestObservations testObservations)
            {
                this.testObservations = testObservations;
            }

            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                // ReSharper disable once MethodSupportsCancellation
                await using var d = cancellationToken.Register(() => testObservations.CancelledRequests.Add(request));

                yield return new(request.Payload + 1);
                yield return new(request.Payload + 2);
                yield return new(request.Payload + 3);
            }
        }

        public interface ITestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
        {
        }

        [HttpInteractiveStreamingRequest]
        public sealed record TestRequestWithoutPayload;

        private sealed class TestStreamingHandlerWithoutPayload : ITestStreamingHandlerWithoutPayload
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(1);
                yield return new(2);
                yield return new(3);
            }
        }

        public interface ITestStreamingHandlerWithoutPayload : IInteractiveStreamingHandler<TestRequestWithoutPayload, TestItem>
        {
        }

        [HttpInteractiveStreamingRequest]
        public sealed record TestRequestWithCollectionPayload(List<int> Payload);

        private sealed class TestStreamingHandlerWithCollectionPayload : ITestStreamingHandlerWithCollectionPayload
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithCollectionPayload request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(request.Payload.Sum() + 1);
                yield return new(request.Payload.Sum() + 2);
                yield return new(request.Payload.Sum() + 3);
            }
        }

        public interface ITestStreamingHandlerWithCollectionPayload : IInteractiveStreamingHandler<TestRequestWithCollectionPayload, TestItem>
        {
        }

        [HttpInteractiveStreamingRequest]
        public sealed record TestRequestWithCustomSerializedItemType(int Payload);

        public sealed record TestItemWithCustomSerializedPayload(TestItemCustomSerializedPayload Payload);

        public sealed record TestItemCustomSerializedPayload(int Payload);

        private sealed class TestStreamingHandlerWithCustomSerializedItemType : ITestStreamingHandlerWithCustomSerializedItemType
        {
            public async IAsyncEnumerable<TestItemWithCustomSerializedPayload> ExecuteRequest(TestRequestWithCustomSerializedItemType request,
                                                                                              [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(new(request.Payload + 1));
                yield return new(new(request.Payload + 2));
                yield return new(new(request.Payload + 3));
            }
        }

        public interface ITestStreamingHandlerWithCustomSerializedItemType : IInteractiveStreamingHandler<TestRequestWithCustomSerializedItemType, TestItemWithCustomSerializedPayload>
        {
        }

        internal sealed class TestItemJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestItemCustomSerializedPayload);

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return Activator.CreateInstance(typeof(TestItemConverter)) as JsonConverter;
            }
        }

        internal sealed class TestItemConverter : JsonConverter<TestItemCustomSerializedPayload>
        {
            public override TestItemCustomSerializedPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestItemCustomSerializedPayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }

        [HttpInteractiveStreamingRequest]
        public sealed record TestRequestWithError;

        private sealed class TestStreamingHandlerWithError : ITestStreamingHandlerWithError
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithError request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(1);
                throw new InvalidOperationException("test");
            }
        }

        public interface ITestStreamingHandlerWithError : IInteractiveStreamingHandler<TestRequestWithError, TestItem>
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

        private sealed class TestObservations
        {
            public BlockingCollection<TestRequest> CancelledRequests { get; } = new();
        }
    }
}
