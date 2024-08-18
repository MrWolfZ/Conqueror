using System.Collections.Concurrent;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conqueror.Streaming.Transport.Http.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request, response, and interface types must be public for dynamic type generation to work")]
public sealed class StreamingHttpClientTests : TestBase
{
    private const string ErrorPayload = "{\"Message\":\"this is an error\"}";

    private int? customResponseStatusCode;
    private Exception? webSocketFactoryException;

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandler>();

        var result = await handler.ExecuteRequest(new(10), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public void GivenFailedWebSocketConnection_ThrowsHttpStreamFailedException()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandler>();

        customResponseStatusCode = StatusCodes.Status402PaymentRequired;

        var ex = Assert.ThrowsAsync<HttpStreamFailedException>(() => handler.ExecuteRequest(new(10)).Drain());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.InnerException, Is.Not.Null);
        Assert.That(ex.InnerException, Is.InstanceOf<InvalidOperationException>());

        // we cannot assert on the status code property of the HttpStreamFailedException since that is
        // only populated when using a real websocket client
        Assert.That(ex.InnerException.Message, Is.EqualTo($"Incomplete handshake, status code: {customResponseStatusCode}"));
    }

    [Test]
    public void GivenExceptionDuringWebSocketCreation_ThrowsHttpStreamFailedException()
    {
        webSocketFactoryException = new();

        var handler = ResolveOnClient<ITestStreamingRequestHandler>();

        var ex = Assert.ThrowsAsync<HttpStreamFailedException>(() => handler.ExecuteRequest(new(10)).Drain());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex?.StatusCode, Is.Null);
        Assert.That(ex?.InnerException, Is.SameAs(webSocketFactoryException));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnectionWithoutPayload_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithoutPayload>();

        var result = await handler.ExecuteRequest(new(), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnectionWithCustomSerializedItemType_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithCustomSerializedItemType>();

        var result = await handler.ExecuteRequest(new(10), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnectionWithCollectionPayload_StreamsItems()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithCollectionPayload>();

        var result = await handler.ExecuteRequest(new([10, 11]), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 22, 23, 24 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenErrorOccursOnServer_HttpStreamingExceptionIsThrown()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandlerWithError>();

        using var cts = new CancellationTokenSource();

        var stream = handler.ExecuteRequest(new(), TestTimeoutToken);

        var enumerator = stream.GetAsyncEnumerator(cts.Token);

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        _ = Assert.ThrowsAsync<HttpStreamFailedException>(() => enumerator.MoveNextAsync().AsTask());
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenCancellingRead_CancellationIsPropagatedToServer()
    {
        var handler = ResolveOnClient<ITestStreamingRequestHandler>();

        using var cts = new CancellationTokenSource();

        var stream = handler.ExecuteRequest(new(10), TestTimeoutToken);

        var enumerator = stream.GetAsyncEnumerator(cts.Token);

        _ = await enumerator.MoveNextAsync();

        await cts.CancelAsync();

        Resolve<TestObservations>().CancelledRequests.ShouldReceiveItem(new(10));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenCancellingInParallel_DoesNotThrowException()
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
        _ = services.AddMvc().AddConquerorStreamingHttpControllers();
        _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestItemJsonConverterFactory()); });

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithoutPayload>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCollectionPayload>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithCustomSerializedItemType>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithError>()
                    .AddConquerorStreamingRequestHandler<NonHttpTestStreamingRequestHandler>()
                    .AddSingleton<TestObservations>();
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorStreamingHttpClientServices(o =>
        {
            _ = o.UseWebSocketFactory((uri, headers, _) =>
            {
                if (webSocketFactoryException is not null)
                {
                    throw webSocketFactoryException;
                }

                return ConnectToWebSocket(uri.AbsolutePath, headers);
            });

            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };
        });

        _ = services.AddConquerorStreamingRequestClient<ITestStreamingRequestHandler>(b => b.UseWebSocket(new("http://example")))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandlerWithoutPayload>(b => b.UseWebSocket(new("http://example")))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandlerWithCollectionPayload>(b => b.UseWebSocket(new("http://example")))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandlerWithCustomSerializedItemType>(b => b.UseWebSocket(new("http://example"), o => o.JsonSerializerOptions = new()
                    {
                        Converters = { new TestItemJsonConverterFactory() },
                        PropertyNameCaseInsensitive = true,
                    }))
                    .AddConquerorStreamingRequestClient<ITestStreamingRequestHandlerWithError>(b => b.UseWebSocket(new("http://example")));
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (ctx, next) =>
        {
            if (customResponseStatusCode != null)
            {
                ctx.Response.StatusCode = customResponseStatusCode.Value;
                ctx.Response.ContentType = MediaTypeNames.Application.Json;
                await using var streamWriter = new StreamWriter(ctx.Response.Body);
                await streamWriter.WriteAsync(ErrorPayload);
                return;
            }

            await next();
        });

        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpStream]
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

    [HttpStream]
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

    [HttpStream]
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

    [HttpStream]
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

    [HttpStream]
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
