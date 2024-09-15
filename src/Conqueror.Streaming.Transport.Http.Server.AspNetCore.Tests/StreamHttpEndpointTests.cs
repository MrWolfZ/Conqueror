using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Conqueror.Streaming.Transport.Http.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class StreamHttpEndpointTests : TestBase
{
    [Test]
    public async Task GivenHttpStreamingRequest_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/test");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequest, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequest_WhenClosingConnectionBeforeEndOfStream_ThenConnectionIsClosedSuccessfully()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/test");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequest, TestItem>(jsonWebSocket);

        var enumerator = streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator();

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        _ = await enumerator.MoveNextAsync();

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        _ = await enumerator.MoveNextAsync();

        Assert.DoesNotThrowAsync(() => streamingClientWebSocket.Close(TestTimeoutToken));
    }

    [Test]
    public async Task GivenStreamHandlerWebsocketEndpoint_WhenCancelingEnumeration_ThenReadingFromSocketIsCanceled()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/test");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequest, TestItem>(jsonWebSocket);

        using var cts = new CancellationTokenSource();

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        var readTask = streamingClientWebSocket.Read(cts.Token).GetAsyncEnumerator(cts.Token).MoveNextAsync();

        await cts.CancelAsync();

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => readTask.AsTask());
    }

    [Test]
    public async Task GivenHttpStreamingRequestWithoutPayload_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/testStreamingRequestWithoutPayload");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequestWithoutPayload, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequestWithComplexPayload_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/testStreamingRequestWithComplexPayload");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequestWithComplexPayload, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(new(10)), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequestWithCustomSerializedPayloadType_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/testStreamingRequestWithCustomSerializedPayloadType");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequestWithCustomSerializedPayloadType, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(new(10)), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenCustomPathConvention_WhenCallingEndpointsWithPathAccordingToConvention_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket1 = await ConnectToWebSocket("/api/streams/testStreamingRequest3FromConvention");
        var webSocket2 = await ConnectToWebSocket("/api/streams/testStreamingRequest4FromConvention");
        using var socket1 = new TextWebSocket(webSocket1);
        using var socket2 = new TextWebSocket(webSocket2);
        using var textWebSocket1 = new TextWebSocketWithHeartbeat(socket1, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var textWebSocket2 = new TextWebSocketWithHeartbeat(socket2, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket1 = new JsonWebSocket(textWebSocket1, JsonSerializerOptions);
        using var jsonWebSocket2 = new JsonWebSocket(textWebSocket2, JsonSerializerOptions);
        using var streamingClientWebSocket1 = new StreamingClientWebSocket<TestStreamingRequest3, TestItem>(jsonWebSocket1);
        using var streamingClientWebSocket2 = new StreamingClientWebSocket<TestStreamingRequest4, TestItem>(jsonWebSocket2);

        using var observedItems1 = new BlockingCollection<TestItem>();
        using var observedItems2 = new BlockingCollection<TestItem>();

        var receiveLoopTask1 = ReadFromSocket(streamingClientWebSocket1, observedItems1);
        var receiveLoopTask2 = ReadFromSocket(streamingClientWebSocket2, observedItems2);

        _ = await streamingClientWebSocket1.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems1.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket2.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems2.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket1.RequestNextItem(TestTimeoutToken);
        observedItems1.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket2.RequestNextItem(TestTimeoutToken);
        observedItems2.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket1.RequestNextItem(TestTimeoutToken);
        observedItems1.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket2.RequestNextItem(TestTimeoutToken);
        observedItems2.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket1.RequestNextItem(TestTimeoutToken);
        observedItems1.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        _ = await streamingClientWebSocket2.RequestNextItem(TestTimeoutToken);
        observedItems2.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask1;
        await receiveLoopTask2;
    }

    [Test]
    public async Task GivenCustomHttpStreamController_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/custom/streams/test");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequest, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenCustomHttpStreamControllerWithoutPayload_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/custom/streams/testStreamingRequestWithoutPayload");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequestWithoutPayload, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequestWithCustomPath_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/testStreamingRequestWithCustomPath");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequestWithCustomPath, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequestWithVersion_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/v2/streams/testStreamingRequestWithVersion");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestStreamingRequestWithVersion, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequestWithDelegateHandler_WhenCallingEndpoint_ItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/testDelegate");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestDelegateStreamingRequest, TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.SendInitialRequest(new(10), TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(11));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(12));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldReceiveItem(new(13));

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await receiveLoopTask;
    }

    [Test]
    public async Task GivenHttpStreamingRequest_WhenExceptionOccursInSourceEnumerable_ThenErrorMessageIsReceivedAndConnectionIsClosed()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/testRequestWithError");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestRequestWithError, TestItem>(jsonWebSocket);

        _ = await streamingClientWebSocket.SendInitialRequest(new(), TestTimeoutToken);

        var enumerator = streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator();

        // successful invocation
        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);

        // error
        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        Assert.That(enumerator.Current, Is.InstanceOf<ErrorMessage>());

        // should finish the enumeration
        Assert.That(await enumerator.MoveNextAsync(), Is.False);
    }

    [Test]
    public async Task GivenStreamHandlerWebsocketEndpoint_WhenClientDoesNotReadTheEnumerable_ThenTheServerCanStillSuccessfullyCloseTheConnection()
    {
        var webSocket = await ConnectToWebSocket("/api/streams/testRequestWithOneItem");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestRequestWithOneItem, TestItem>(jsonWebSocket);

        _ = await streamingClientWebSocket.SendInitialRequest(new(), TestTimeoutToken);

        var enumerator = streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator();

        // successful invocation
        Assert.That(await enumerator.MoveNextAsync(), Is.True);

        // this causes the server to close the connection
        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);

        // give socket the time to close
        var attempts = 0;
        while (socket.State != WebSocketState.Closed && attempts++ < 20)
        {
            await Task.Delay(10);
        }

        Assert.That(socket.State, Is.EqualTo(WebSocketState.Closed));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var applicationPartManager = new ApplicationPartManager();
        applicationPartManager.ApplicationParts.Add(new TestControllerApplicationPart());
        applicationPartManager.FeatureProviders.Add(new TestControllerFeatureProvider());

        _ = services.AddSingleton(applicationPartManager);

        _ = services.AddMvc().AddConquerorStreamingHttpControllers(o => o.PathConvention = new TestHttpStreamPathConvention());
        _ = services.PostConfigure<JsonOptions>(options => { options.JsonSerializerOptions.Converters.Add(new TestStreamingRequestWithCustomSerializedPayloadTypeHandler.PayloadJsonConverterFactory()); });

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamingRequestHandler2>()
                    .AddConquerorStreamProducer<TestStreamingRequestHandler3>()
                    .AddConquerorStreamProducer<TestStreamingRequestHandler4>()
                    .AddConquerorStreamProducer<TestStreamProducerWithoutPayload>()
                    .AddConquerorStreamProducer<TestStreamProducerWithComplexPayload>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithCustomSerializedPayloadTypeHandler>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithCustomPathHandler>()
                    .AddConquerorStreamProducer<TestStreamingRequestWithVersionHandler>()
                    .AddConquerorStreamProducer<TestStreamProducerWithError>()
                    .AddConquerorStreamProducer<TestStreamProducerWithOneItem>()
                    .AddConquerorStreamProducerDelegate<TestDelegateStreamingRequest, TestDelegateItem>((command, _, cancellationToken) =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return AsyncEnumerableHelper.Of(new TestDelegateItem(command.Payload + 1), new TestDelegateItem(command.Payload + 2), new TestDelegateItem(command.Payload + 3));
                    });
    }

    private JsonSerializerOptions JsonSerializerOptions => Resolve<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private async Task ReadFromSocket<TRequest, TItem>(StreamingClientWebSocket<TRequest, TItem> socket, BlockingCollection<TItem> receivedItems)
        where TRequest : class
    {
        await foreach (var msg in socket.Read(TestTimeoutToken))
        {
            if (msg is StreamingMessageEnvelope<TItem> { Message: { } } env)
            {
                receivedItems.Add(env.Message, TestTimeoutToken);
            }
        }
    }

    [HttpStream]
    public sealed record TestStreamingRequest(int Payload);

    public sealed record TestItem(int Payload);

    [HttpStream]
    public sealed record TestStreamingRequest2;

    public sealed record TestItem2;

    [HttpStream]
    public sealed record TestStreamingRequest3(int Payload);

    [HttpStream]
    public sealed record TestStreamingRequest4(int Payload);

    [HttpStream]
    public sealed record TestStreamingRequestWithoutPayload;

    [HttpStream]
    public sealed record TestStreamingRequestWithComplexPayload(TestStreamingRequestWithComplexPayloadPayload Payload);

    public sealed record TestStreamingRequestWithComplexPayloadPayload(int Payload);

    [HttpStream]
    public sealed record TestStreamingRequestWithCustomSerializedPayloadType(TestStreamingRequestWithCustomSerializedPayloadTypePayload Payload);

    public sealed record TestStreamingRequestWithCustomSerializedPayloadTypeResponse(TestStreamingRequestWithCustomSerializedPayloadTypePayload Payload);

    public sealed record TestStreamingRequestWithCustomSerializedPayloadTypePayload(int Payload);

    [HttpStream(Path = "/api/testStreamingRequestWithCustomPath")]
    public sealed record TestStreamingRequestWithCustomPath(int Payload);

    [HttpStream(Version = "v2")]
    public sealed record TestStreamingRequestWithVersion(int Payload);

    [HttpStream]
    public sealed record TestDelegateStreamingRequest(int Payload);

    public sealed record TestDelegateItem(int Payload);

    [HttpStream]
    public sealed record TestRequestWithError;

    [HttpStream]
    public sealed record TestRequestWithOneItem;

    public interface ITestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>;

    public interface ITestStreamingRequestWithCustomSerializedPayloadTypeHandler : IStreamProducer<TestStreamingRequestWithCustomSerializedPayloadType, TestStreamingRequestWithCustomSerializedPayloadTypeResponse>;

    public sealed class TestStreamProducer : ITestStreamProducer
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    public sealed class TestStreamingRequestHandler2 : IStreamProducer<TestStreamingRequest2, TestItem2>
    {
        public IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestStreamingRequestHandler3 : IStreamProducer<TestStreamingRequest3, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest3 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    public sealed class TestStreamingRequestHandler4 : IStreamProducer<TestStreamingRequest4, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest4 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    public sealed class TestStreamProducerWithoutPayload : IStreamProducer<TestStreamingRequestWithoutPayload, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(11);
            yield return new(12);
            yield return new(13);
        }
    }

    public sealed class TestStreamProducerWithComplexPayload : IStreamProducer<TestStreamingRequestWithComplexPayload, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithComplexPayload request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload.Payload + 1);
            yield return new(request.Payload.Payload + 2);
            yield return new(request.Payload.Payload + 3);
        }
    }

    public sealed class TestStreamingRequestWithCustomSerializedPayloadTypeHandler : ITestStreamingRequestWithCustomSerializedPayloadTypeHandler
    {
        public async IAsyncEnumerable<TestStreamingRequestWithCustomSerializedPayloadTypeResponse> ExecuteRequest(TestStreamingRequestWithCustomSerializedPayloadType request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(new(request.Payload.Payload + 1));
            yield return new(new(request.Payload.Payload + 2));
            yield return new(new(request.Payload.Payload + 3));
        }

        internal sealed class PayloadJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(TestStreamingRequestWithCustomSerializedPayloadTypePayload);

            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return Activator.CreateInstance(typeof(PayloadJsonConverter)) as JsonConverter;
            }
        }

        internal sealed class PayloadJsonConverter : JsonConverter<TestStreamingRequestWithCustomSerializedPayloadTypePayload>
        {
            public override TestStreamingRequestWithCustomSerializedPayloadTypePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, TestStreamingRequestWithCustomSerializedPayloadTypePayload value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Payload);
            }
        }
    }

    public sealed class TestStreamingRequestWithCustomPathHandler : IStreamProducer<TestStreamingRequestWithCustomPath, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithCustomPath request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    public sealed class TestStreamingRequestWithVersionHandler : IStreamProducer<TestStreamingRequestWithVersion, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequestWithVersion request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    private sealed class TestStreamProducerWithError : IStreamProducer<TestRequestWithError, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithError request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(1);
            throw new InvalidOperationException("test");
        }
    }

    private sealed class TestStreamProducerWithOneItem : IStreamProducer<TestRequestWithOneItem, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithOneItem request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(1);
        }
    }

    private sealed class TestHttpStreamPathConvention : IHttpStreamPathConvention
    {
        public string? GetStreamPath(Type requestType, HttpStreamAttribute attribute)
        {
            if (requestType != typeof(TestStreamingRequest3) && requestType != typeof(TestStreamingRequest4) && requestType != typeof(TestStreamingRequest2))
            {
                return null;
            }

            return $"/api/streams/{requestType.Name}FromConvention";
        }
    }

    [ApiController]
    private sealed class TestHttpStreamController : ControllerBase
    {
        [HttpGet("/api/custom/streams/test")]
        public Task ExecuteTestStreamingRequest(CancellationToken cancellationToken)
        {
            return HttpStreamExecutor.ExecuteStreamingRequest<TestStreamingRequest, TestItem>(HttpContext, cancellationToken);
        }

        [HttpGet("/api/custom/streams/testStreamingRequestWithoutPayload")]
        public Task ExecuteTestStreamingRequestWithoutPayload(CancellationToken cancellationToken)
        {
            return HttpStreamExecutor.ExecuteStreamingRequest<TestStreamingRequestWithoutPayload, TestItem>(HttpContext, cancellationToken);
        }
    }

    private sealed class TestControllerApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => nameof(TestControllerApplicationPart);

        public IEnumerable<TypeInfo> Types { get; } = [typeof(TestHttpStreamController).GetTypeInfo()];
    }

    private sealed class TestControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType() == typeof(TestHttpStreamController);
    }
}
