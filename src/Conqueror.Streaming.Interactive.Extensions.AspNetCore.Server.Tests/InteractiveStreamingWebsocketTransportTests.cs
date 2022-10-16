using System.Collections.Concurrent;
using System.Diagnostics;
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
        public async Task GivenWebSocketConnection_RequestingItems_StreamsItems()
        {
            var webSocket = await ConnectToWebSocket("api/streams/interactive/test", "?payload=10");
            using var socket = new TextWebSocket(webSocket);
            using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);
            using var streamingClientWebSocket = new StreamingClientWebSocket<TestItem>(jsonWebSocket);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(Debugger.IsAttached ? TimeSpan.FromSeconds(60) : TimeSpan.FromSeconds(1));

            using var observedItems = new BlockingCollection<TestItem>();

            var receiveLoopTask = RunReceiveLoop();

            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldReceiveItem(new(11));
            
            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldReceiveItem(new(12));
            
            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldReceiveItem(new(13));
            
            await streamingClientWebSocket.RequestNextItem(TestTimeoutToken);
            observedItems.ShouldNotReceiveAnyItem();

            await receiveLoopTask;

            async Task RunReceiveLoop()
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        var message = await streamingClientWebSocket.Receive(cts.Token);

                        if (message is StreamingMessageEnvelope<TestItem> { Message: { } } env)
                        {
                            observedItems.Add(env.Message, cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // nothing to do
                    }
                    catch (InvalidOperationException)
                    {
                        // nothing to do
                    }
                }
            }
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddMvc().AddConquerorInteractiveStreaming();

            _ = services.AddTransient<TestStreamingHandler>()
                        .AddTransient<TestStreamingHandlerWithoutPayload>();

            _ = services.AddConquerorInteractiveStreaming().ConfigureConqueror();
        }

        protected override void Configure(IApplicationBuilder app)
        {
            _ = app.UseRouting();
            _ = app.UseEndpoints(b => b.MapControllers());
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

        private sealed record TestRequestWithoutPayload;

        private sealed class TestStreamingHandlerWithoutPayload : IInteractiveStreamingHandler<TestRequestWithoutPayload, TestItem>
        {
            public async IAsyncEnumerable<TestItem> ExecuteRequest(TestRequestWithoutPayload request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield return new(1);
                yield return new(2);
                yield return new(3);
            }
        }
    }
}
