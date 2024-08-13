using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Conqueror.Streaming.Transport.Http.Common;
using Microsoft.AspNetCore.Builder;

// ReSharper disable AccessToDisposedClosure (disposal works)

namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "request and response types must be public for dynamic type generation to work")]
public sealed class StreamingWebsocketTransportTests : TestBase
{
    [Test]
    public async Task GivenStreamHandlerWebsocketEndpoint_WhenConnectingAndRequestingItems_ThenItemsAreStreamedUntilStreamCompletes()
    {
        var webSocket = await ConnectToWebSocket("api/streams/test", "?payload=10");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestItem>(jsonWebSocket);

        using var observedItems = new BlockingCollection<TestItem>();

        var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
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
    public async Task GivenStreamHandlerWebsocketEndpoint_WhenClosingConnectionBeforeEndOfStream_ThenConnectionIsClosedSuccessfully()
    {
        var webSocket = await ConnectToWebSocket("api/streams/test", "?payload=10");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestItem>(jsonWebSocket);

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
        _ = await streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator().MoveNextAsync();

        Assert.DoesNotThrowAsync(() => streamingClientWebSocket.Close(TestTimeoutToken));
    }

    [Test]
    public async Task GivenStreamHandlerWebsocketEndpoint_WhenCancelingEnumeration_ThenReadingFromSocketIsCanceled()
    {
        var webSocket = await ConnectToWebSocket("api/streams/test", "?payload=10");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestItem>(jsonWebSocket);

        using var cts = new CancellationTokenSource();

        var readTask = streamingClientWebSocket.Read(cts.Token).GetAsyncEnumerator(cts.Token).MoveNextAsync();

        cts.Cancel();

        _ = Assert.ThrowsAsync<OperationCanceledException>(() => readTask.AsTask());
    }

    [Test]
    public async Task GivenStreamHandlerWebsocketEndpoint_WhenExceptionOccursInSourceEnumerable_ThenErrorMessageIsReceivedAndConnectionIsClosed()
    {
        var webSocket = await ConnectToWebSocket("api/streams/testRequestWithError", string.Empty);
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestItem>(jsonWebSocket);

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);

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
        var webSocket = await ConnectToWebSocket("api/streams/testRequestWithoutPayload", string.Empty);
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
        using var streamingClientWebSocket = new StreamingClientWebSocket<TestItem>(jsonWebSocket);

        _ = await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);

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
        _ = services.AddMvc().AddConquerorStreaming();

        _ = services.AddTransient<TestStreamingHandler>()
                    .AddTransient<TestStreamingHandlerWithoutPayload>()
                    .AddTransient<TestStreamingHandlerWithError>();

        _ = services.AddConquerorStreaming().FinalizeConquerorRegistrations();
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private async Task ReadFromSocket<T>(StreamingClientWebSocket<T> socket, BlockingCollection<T> receivedItems)
    {
        await foreach (var msg in socket.Read(TestTimeoutToken))
        {
            if (msg is StreamingMessageEnvelope<T> { Message: { } } env)
            {
                receivedItems.Add(env.Message, TestTimeoutToken);
            }
        }
    }

    [HttpStreamingRequest]
    public sealed record TestRequest(int Payload);

    public sealed record TestItem(int Payload);

    private sealed class TestStreamingHandler : IStreamingHandler<TestRequest, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new(request.Payload + 1);
            yield return new(request.Payload + 2);
            yield return new(request.Payload + 3);
        }
    }

    [HttpStreamingRequest]
    public sealed record TestRequestWithoutPayload;

    private sealed class TestStreamingHandlerWithoutPayload : IStreamingHandler<TestRequestWithoutPayload, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new(1);
        }
    }

    [HttpStreamingRequest]
    public sealed record TestRequestWithError;

    private sealed class TestStreamingHandlerWithError : IStreamingHandler<TestRequestWithError, TestItem>
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithError request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return new(1);
            throw new InvalidOperationException("test");
        }
    }
}
