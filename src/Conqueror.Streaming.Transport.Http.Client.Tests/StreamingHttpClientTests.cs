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
        var producer = ResolveOnClient<ITestStreamProducer>();

        var result = await producer.ExecuteRequest(new(10), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public void GivenFailedWebSocketConnection_ThrowsHttpStreamFailedException()
    {
        var producer = ResolveOnClient<ITestStreamProducer>();

        customResponseStatusCode = StatusCodes.Status402PaymentRequired;

        var ex = Assert.ThrowsAsync<HttpStreamFailedException>(() => producer.ExecuteRequest(new(10)).Drain());

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

        var producer = ResolveOnClient<ITestStreamProducer>();

        var ex = Assert.ThrowsAsync<HttpStreamFailedException>(() => producer.ExecuteRequest(new(10)).Drain());

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex?.StatusCode, Is.Null);
        Assert.That(ex?.InnerException, Is.SameAs(webSocketFactoryException));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnectionWithoutPayload_StreamsItems()
    {
        var producer = ResolveOnClient<ITestStreamProducerWithoutPayload>();

        var result = await producer.ExecuteRequest(new(), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnectionWithCustomSerializedItemType_StreamsItems()
    {
        var producer = ResolveOnClient<ITestStreamProducerWithCustomSerializedItemType>();

        var result = await producer.ExecuteRequest(new(10), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload.Payload), Is.EquivalentTo(new[] { 11, 12, 13 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnectionWithCollectionPayload_StreamsItems()
    {
        var producer = ResolveOnClient<ITestStreamProducerWithCollectionPayload>();

        var result = await producer.ExecuteRequest(new([10, 11]), TestTimeoutToken).Drain();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Select(i => i.Payload), Is.EquivalentTo(new[] { 22, 23, 24 }));
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenErrorOccursOnServer_HttpStreamingExceptionIsThrown()
    {
        var producer = ResolveOnClient<ITestStreamProducerWithError>();

        using var cts = new CancellationTokenSource();

        var stream = producer.ExecuteRequest(new(), TestTimeoutToken);

        var enumerator = stream.GetAsyncEnumerator(cts.Token);

        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        _ = Assert.ThrowsAsync<HttpStreamFailedException>(() => enumerator.MoveNextAsync().AsTask());
    }

    [Test]
    public async Task GivenSuccessfulWebSocketConnection_WhenCancellingRead_CancellationIsPropagatedToServer()
    {
        var producer = ResolveOnClient<ITestStreamProducer>();

        using var cts = new CancellationTokenSource();

        var stream = producer.ExecuteRequest(new(10), TestTimeoutToken);

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
            var producer = ResolveOnClient<ITestStreamProducer>();

            using var cts = new CancellationTokenSource();

            var stream = producer.ExecuteRequest(new(10), TestTimeoutToken);

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

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducerWithoutPayload>()
                    .AddConquerorStreamProducer<TestStreamProducerWithCollectionPayload>()
                    .AddConquerorStreamProducer<TestStreamProducerWithCustomSerializedItemType>()
                    .AddConquerorStreamProducer<TestStreamProducerWithError>()
                    .AddConquerorStreamProducer<NonHttpTestStreamProducer>()
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

        _ = services.AddConquerorStreamProducerClient<ITestStreamProducer>(b => b.UseWebSocket(new("http://example")))
                    .AddConquerorStreamProducerClient<ITestStreamProducerWithoutPayload>(b => b.UseWebSocket(new("http://example")))
                    .AddConquerorStreamProducerClient<ITestStreamProducerWithCollectionPayload>(b => b.UseWebSocket(new("http://example")))
                    .AddConquerorStreamProducerClient<ITestStreamProducerWithCustomSerializedItemType>(b => b.UseWebSocket(new("http://example"), o => o.JsonSerializerOptions = new()
                    {
                        Converters = { new TestItemJsonConverterFactory() },
                        PropertyNameCaseInsensitive = true,
                    }))
                    .AddConquerorStreamProducerClient<ITestStreamProducerWithError>(b => b.UseWebSocket(new("http://example")));
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

    private sealed class TestStreamProducer(TestObservations observations) : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // ReSharper disable once MethodSupportsCancellation
            await using var d = cancellationToken.Register(() => observations.CancelledRequests.Add(request));

            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    public interface ITestStreamProducer : IStreamProducer<TestRequest, TestItem>
    {
    }

    [HttpStream]
    public sealed record TestRequestWithoutPayload;

    private sealed class TestStreamProducerWithoutPayload : ITestStreamProducerWithoutPayload
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

    public interface ITestStreamProducerWithoutPayload : IStreamProducer<TestRequestWithoutPayload, TestItem>
    {
    }

    [HttpStream]
    public sealed record TestRequestWithCollectionPayload(List<int> Payload);

    private sealed class TestStreamProducerWithCollectionPayload : ITestStreamProducerWithCollectionPayload
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

    public interface ITestStreamProducerWithCollectionPayload : IStreamProducer<TestRequestWithCollectionPayload, TestItem>
    {
    }

    [HttpStream]
    public sealed record TestRequestWithCustomSerializedItemType(int Payload);

    public sealed record TestItemWithCustomSerializedPayload(TestItemCustomSerializedPayload Payload);

    public sealed record TestItemCustomSerializedPayload(int Payload);

    private sealed class TestStreamProducerWithCustomSerializedItemType : ITestStreamProducerWithCustomSerializedItemType
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

    public interface ITestStreamProducerWithCustomSerializedItemType : IStreamProducer<TestRequestWithCustomSerializedItemType, TestItemWithCustomSerializedPayload>
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

    private sealed class TestStreamProducerWithError : ITestStreamProducerWithError
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithError request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(1);
            throw new InvalidOperationException("test");
        }
    }

    public interface ITestStreamProducerWithError : IStreamProducer<TestRequestWithError, TestItem>
    {
    }

    public sealed record NonHttpTestRequest
    {
        public int Payload { get; init; }
    }

    private sealed class NonHttpTestStreamProducer : INonHttpTestStreamProducer
    {
        public IAsyncEnumerable<TestItem> ExecuteRequest(NonHttpTestRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public interface INonHttpTestStreamProducer : IStreamProducer<NonHttpTestRequest, TestItem>
    {
    }

    private sealed class TestObservations
    {
        public BlockingCollection<TestRequest> CancelledRequests { get; } = new();
    }
}
