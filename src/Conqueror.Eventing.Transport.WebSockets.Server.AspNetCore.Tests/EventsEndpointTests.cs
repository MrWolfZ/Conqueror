using System.Collections.Concurrent;
using System.Text.Json;
using Conqueror.Common.Transport.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore.Tests;

[TestFixture]
public sealed class EventsEndpointTests : TestBase
{
    private string? endpointPath;

    [Test]
    public async Task GivenSingleEventTypeId_EventsAreSentToWebSocket()
    {
        var webSocket = await ConnectToWebSocket("api/events", "?eventTypeId=testEvent");
        using var socket = new TextWebSocket(webSocket);
        using var textWebSocket = new TextWebSocketWithHeartbeat(socket, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
        using var jsonWebSocket = new JsonWebSocket(textWebSocket, JsonSerializerOptions);

        using var observedItems = new BlockingCollection<TestEvent>();
        var dispatcher = Resolve<IConquerorEventDispatcher>();

        var testEvent1 = new TestEvent { Payload = 11 };
        var testEvent2 = new TestEvent { Payload = 12 };
        var testEvent3 = new TestEvent { Payload = 13 };

        var receiveLoopTask = ReadFromSocket(jsonWebSocket, observedItems);

        await dispatcher.DispatchEvent(testEvent1, TestTimeoutToken);
        observedItems.ShouldReceiveItem(testEvent1);

        await dispatcher.DispatchEvent(testEvent2, TestTimeoutToken);
        observedItems.ShouldReceiveItem(testEvent2);

        await dispatcher.DispatchEvent(testEvent3, TestTimeoutToken);
        observedItems.ShouldReceiveItem(testEvent3);

        observedItems.ShouldNotReceiveAnyItem(TimeSpan.FromMilliseconds(10));

        await jsonWebSocket.Close(TestTimeoutToken);

        await receiveLoopTask;

        // TODO: test for custom path
        endpointPath = null;
    }
    
    // TODO: test for non-matching event type ID
    // TODO: test for multiple event type IDs
    // TODO: test for mixed matching and non-matching event type IDs

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorEventingWebSocketsControllers(o => o.EndpointPath = endpointPath);

        _ = services.AddConquerorEventingWebSocketsTransportPublisher();
    }

    private JsonSerializerOptions JsonSerializerOptions => Resolve<IOptions<JsonOptions>>().Value.JsonSerializerOptions;

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    private async Task ReadFromSocket(JsonWebSocket socket, BlockingCollection<TestEvent> receivedItems)
    {
        try
        {
            await foreach (var msg in socket.Read("type", _ => typeof(MessageEnvelope), TestTimeoutToken))
            {
                if (msg is MessageEnvelope env)
                {
                    var evt = ((JsonElement)env.Message).Deserialize<TestEvent>(JsonSerializerOptions)!;
                    receivedItems.Add(evt, TestTimeoutToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // nothing to do
        }
    }

    [WebSocketsEvent(EventTypeId = "testEvent")]
    private sealed record TestEvent
    {
        public int Payload { get; init; }
    }
}
