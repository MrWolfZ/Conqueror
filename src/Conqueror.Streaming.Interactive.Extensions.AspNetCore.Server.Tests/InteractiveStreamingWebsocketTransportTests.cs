﻿using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Conqueror.Streaming.Interactive.Extensions.AspNetCore.Common;
using Microsoft.AspNetCore.Builder;

// ReSharper disable AccessToDisposedClosure (disposal works)

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Server.Tests
{
    [TestFixture]
    public sealed class InteractiveStreamingWebsocketTransportTests : TestBase
    {
        [Test]
        public async Task GivenStreamHandlerWebsocketEndpoint_WhenConnectingAndRequestingItems_ThenItemsAreStreamedUntilStreamCompletes()
        {
            var webSocket = await ConnectToWebSocket("api/streams/interactive/test", "?payload=10");
            using var socket = new TextWebSocket(webSocket);
            using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
            using var streamingClientWebSocket = new InteractiveStreamingClientWebSocket<TestItem>(jsonWebSocket);

            using var observedItems = new BlockingCollection<TestItem>();

            var receiveLoopTask = ReadFromSocket(streamingClientWebSocket, observedItems);

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldReceiveItem(new(11));

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldReceiveItem(new(12));

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldReceiveItem(new(13));

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

            await receiveLoopTask;
        }
        
        [Test]
        public async Task GivenStreamHandlerWebsocketEndpoint_WhenClosingConnectionBeforeEndOfStream_ThenConnectionIsClosedSuccessfully()
        {
            var webSocket = await ConnectToWebSocket("api/streams/interactive/test", "?payload=10");
            using var socket = new TextWebSocket(webSocket);
            using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
            using var streamingClientWebSocket = new InteractiveStreamingClientWebSocket<TestItem>(jsonWebSocket);

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            _ = await streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator().MoveNextAsync();

            Assert.DoesNotThrowAsync(() => streamingClientWebSocket.Close(TestTimeoutToken));
        }
        
        [Test]
        public async Task GivenStreamHandlerWebsocketEndpoint_WhenCancelingEnumeration_ThenReadingFromSocketIsCanceled()
        {
            var webSocket = await ConnectToWebSocket("api/streams/interactive/test", "?payload=10");
            using var socket = new TextWebSocket(webSocket);
            using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
            using var streamingClientWebSocket = new InteractiveStreamingClientWebSocket<TestItem>(jsonWebSocket);

            using var cts = new CancellationTokenSource();

            var readTask = streamingClientWebSocket.Read(cts.Token).GetAsyncEnumerator(cts.Token).MoveNextAsync();
            
            cts.Cancel();

            _ = Assert.ThrowsAsync<OperationCanceledException>(() => readTask.AsTask());
        }
        
        [Test]
        public async Task GivenStreamHandlerWebsocketEndpoint_WhenExceptionOccursInSourceEnumerable_ThenErrorMessageIsReceivedAndConnectionIsClosed()
        {
            var webSocket = await ConnectToWebSocket("api/streams/interactive/testRequestWithError", string.Empty);
            using var socket = new TextWebSocket(webSocket);
            using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
            using var streamingClientWebSocket = new InteractiveStreamingClientWebSocket<TestItem>(jsonWebSocket);

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            
            var enumerator = streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator();
            
            // successful invocation
            Assert.IsTrue(await enumerator.MoveNextAsync());

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            
            // error
            Assert.IsTrue(await enumerator.MoveNextAsync());
            
            Assert.IsInstanceOf<ErrorMessage>(enumerator.Current);
            
            // should finish the enumeration 
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }
        
        [Test]
        public async Task GivenStreamHandlerWebsocketEndpoint_WhenClientDoesNotReadTheEnumerable_ThenTheServerCanStillSuccessfullyCloseTheConnection()
        {
            var webSocket = await ConnectToWebSocket("api/streams/interactive/testRequestWithoutPayload", string.Empty);
            using var socket = new TextWebSocket(webSocket);
            using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
            using var streamingClientWebSocket = new InteractiveStreamingClientWebSocket<TestItem>(jsonWebSocket);

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            
            var enumerator = streamingClientWebSocket.Read(TestTimeoutToken).GetAsyncEnumerator();
            
            // successful invocation
            Assert.IsTrue(await enumerator.MoveNextAsync());

            // this causes the server to close the connection
            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);

            // give socket the time to close
            await Task.Delay(10);
            
            Assert.AreEqual(WebSocketState.Closed, socket.State);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            _ = services.AddTransient<TestStreamingHandler>()
                        .AddTransient<TestStreamingHandlerWithoutPayload>()
                        .AddTransient<TestStreamingHandlerWithError>();

            _ = services.AddConquerorInteractiveStreaming().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
        }

        private async Task ReadFromSocket<T>(InteractiveStreamingClientWebSocket<T> socket, BlockingCollection<T> receivedItems)
        {
            await foreach (var msg in socket.Read(TestTimeoutToken))
            {
                if (msg is StreamingMessageEnvelope<T> { Message: { } } env)
                {
                    receivedItems.Add(env.Message, TestTimeoutToken);
                }
            }
        }

// request and response types must be public for dynamic type generation to work
#pragma warning disable CA1034

        [HttpInteractiveStream]
        public sealed record TestRequest(int Payload);

        public sealed record TestItem(int Payload);

        private sealed class TestStreamingHandler : IInteractiveStreamingHandler<TestRequest, TestItem>
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield return new(request.Payload + 1);
                yield return new(request.Payload + 2);
                yield return new(request.Payload + 3);
            }
        }

        [HttpInteractiveStream]
        public sealed record TestRequestWithoutPayload;

        private sealed class TestStreamingHandlerWithoutPayload : IInteractiveStreamingHandler<TestRequestWithoutPayload, TestItem>
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield return new(1);
            }
        }

        [HttpInteractiveStream]
        public sealed record TestRequestWithError;

        private sealed class TestStreamingHandlerWithError : IInteractiveStreamingHandler<TestRequestWithError, TestItem>
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithError request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield return new(1);
                throw new InvalidOperationException("test");
            }
        }
    }
}
