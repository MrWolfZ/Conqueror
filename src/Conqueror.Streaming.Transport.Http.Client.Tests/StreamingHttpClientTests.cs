using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Transport.Http.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
public sealed class StreamingHttpClientTests : TestBase
{
    [Test]
    public async Task GivenStreamingRequest_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandler>();

        var result = await handler.ExecuteRequest(new(10), TestTimeoutToken).Drain(TestTimeoutToken);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EqualTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public async Task GivenStreamingRequestWithoutPayload_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithoutPayload>();

        var result = await handler.ExecuteRequest(new(), TestTimeoutToken).Drain(TestTimeoutToken);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenStreamingRequestWithCustomSerializedItemType_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithCustomSerializedItemType>();

        var result = await handler.ExecuteRequest(new(10), TestTimeoutToken).Drain(TestTimeoutToken);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload.Payload), Is.EqualTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public async Task GivenStreamingRequestWithCollectionPayload_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithCollectionPayload>();

        var result = await handler.ExecuteRequest(new(new() { 10, 11 }), TestTimeoutToken).Drain(TestTimeoutToken);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EqualTo(new[] { 22, 23, 24 }));
    }

    [Test]
    public async Task GivenStreamingRequest_WhenErrorOccursOnServer_HttpStreamingExceptionIsThrown()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithError>();

        using var cts = new CancellationTokenSource();

        var stream = handler.ExecuteRequest(new(), TestTimeoutToken);

        var enumerator = stream.GetAsyncEnumerator(cts.Token);

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        _ = Assert.ThrowsAsync<HttpStreamingException>(() => enumerator.MoveNextAsync().AsTask());
    }

    [Test]
    public async Task GivenStreamingRequest_WhenCancellingRead_CancellationIsPropagatedToServer()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandler>();

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
            var handler = ResolveOnClient<ITestStreamingRequestHandler>();

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
        _ = services.AddMvc().AddConquerorStreaming();
        _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestItemJsonConverterFactory()); });

        _ = services.AddTransient<TestStreamingRequestHandler>()
                    .AddTransient<TestStreamingRequestHandlerWithoutPayload>()
                    .AddTransient<TestStreamingRequestHandlerWithCollectionPayload>()
                    .AddTransient<TestStreamingRequestHandlerWithCustomSerializedItemType>()
                    .AddTransient<TestStreamingRequestHandlerWithError>()
                    .AddTransient<NonHttpTestStreamingRequestHandler>()
                    .AddSingleton<TestObservations>();

        _ = services.AddConquerorStreaming();
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorStreamingHttpClientServices(o =>
        {
            o.WebSocketFactory = (uri, _) => ConnectToWebSocket(uri.AbsolutePath, uri.Query);

            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        });

        _ = services.AddConquerorStreamingHttpClient<ITestStreamingRequestHandler>(_ => new("http://example"))
                    .AddConquerorStreamingHttpClient<ITestStreamingRequestHandlerWithoutPayload>(_ => new("http://example"))
                    .AddConquerorStreamingHttpClient<ITestStreamingRequestHandlerWithCollectionPayload>(_ => new("http://example"))
                    .AddConquerorStreamingHttpClient<ITestStreamingRequestHandlerWithCustomSerializedItemType>(_ => new("http://example"), o => o.JsonSerializerOptions = new()
                    {
                        Converters = { new TestItemJsonConverterFactory() },
                        PropertyNameCaseInsensitive = true,
                    })
                    .AddConquerorStreamingHttpClient<ITestStreamingRequestHandlerWithError>(_ => new("http://example"));
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpStreamingRequest]
    public sealed record TestRequest(int Payload);

    public sealed record TestItem(int Payload);

    private sealed class TestStreamingRequestHandler : ITestStreamingRequestHandler
    {
        private readonly TestObservations testObservations;

        public TestStreamingRequestHandler(TestObservations testObservations)
        {
            this.testObservations = testObservations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // ReSharper disable once MethodSupportsCancellation
            await using var d = cancellationToken.Register(() => testObservations.CancelledRequests.Add(request));

            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    public interface ITestStreamingRequestHandler : IStreamingRequestHandler<TestRequest, TestItem>
    {
    }

    [HttpStreamingRequest]
    public sealed record TestRequestWithoutPayload;

    private sealed class TestStreamingRequestHandlerWithoutPayload : ITestStreamingRequestHandlerWithoutPayload
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(1);
            yield return new(2);
            yield return new(3);
        }
    }

    public interface ITestStreamingRequestHandlerWithoutPayload : IStreamingRequestHandler<TestRequestWithoutPayload, TestItem>
    {
    }

    [HttpStreamingRequest]
    public sealed record TestRequestWithCollectionPayload(List<int> Payload);

    private sealed class TestStreamingRequestHandlerWithCollectionPayload : ITestStreamingRequestHandlerWithCollectionPayload
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithCollectionPayload request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload.Sum() + 1);
            yield return new(request.Payload.Sum() + 2);
            yield return new(request.Payload.Sum() + 3);
        }
    }

    public interface ITestStreamingRequestHandlerWithCollectionPayload : IStreamingRequestHandler<TestRequestWithCollectionPayload, TestItem>
    {
    }

    [HttpStreamingRequest]
    public sealed record TestRequestWithCustomSerializedItemType(int Payload);

    public sealed record TestItemWithCustomSerializedPayload(TestItemCustomSerializedPayload Payload);

    public sealed record TestItemCustomSerializedPayload(int Payload);

    private sealed class TestStreamingRequestHandlerWithCustomSerializedItemType : ITestStreamingRequestHandlerWithCustomSerializedItemType
    {
        public async IAsyncEnumerable<TestItemWithCustomSerializedPayload> ExecuteRequest(TestRequestWithCustomSerializedItemType request,
                                                                                          [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(new(request.Payload + 1));
            yield return new(new(request.Payload + 2));
            yield return new(new(request.Payload + 3));
        }
    }

    public interface ITestStreamingRequestHandlerWithCustomSerializedItemType : IStreamingRequestHandler<TestRequestWithCustomSerializedItemType, TestItemWithCustomSerializedPayload>
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

    [HttpStreamingRequest]
    public sealed record TestRequestWithError;

    private sealed class TestStreamingRequestHandlerWithError : ITestStreamingRequestHandlerWithError
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithError request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(1);
            throw new InvalidOperationException("test");
        }
    }

    public interface ITestStreamingRequestHandlerWithError : IStreamingRequestHandler<TestRequestWithError, TestItem>
    {
    }

    public sealed record NonHttpTestRequest
    {
        public int Payload { get; init; }
    }

    private sealed class NonHttpTestStreamingRequestHandler : INonHttpTestStreamingRequestHandler
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(NonHttpTestRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public interface INonHttpTestStreamingRequestHandler : IStreamingRequestHandler<NonHttpTestRequest, TestItem>
    {
    }

    private sealed class TestObservations
    {
        public BlockingCollection<TestRequest> CancelledRequests { get; } = new();
    }
}
