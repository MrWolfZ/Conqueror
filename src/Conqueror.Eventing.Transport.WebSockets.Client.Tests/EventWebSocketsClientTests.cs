using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;

namespace Conqueror.Eventing.Transport.WebSockets.Client.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for custom interface generation")]
public sealed class EventWebSocketsClientTests : TestBase, IDisposable
{
    private readonly BlockingCollection<object> receivedEvents = new();

    [Test]
    public async Task GivenEventPublishedOnServer_EventIsReceivedOnClient()
    {
        var dispatcher = Resolve<IConquerorEventDispatcher>();

        var testEvent1 = new TestEvent { Payload = 11 };
        var testEvent2 = new TestEvent { Payload = 12 };
        var testEvent3 = new TestEvent { Payload = 13 };

        await dispatcher.DispatchEvent(testEvent1);
        await dispatcher.DispatchEvent(testEvent2);

        receivedEvents.ShouldReceiveItem(testEvent1);
        receivedEvents.ShouldReceiveItem(testEvent1);

        receivedEvents.ShouldReceiveItem(testEvent2);
        receivedEvents.ShouldReceiveItem(testEvent2);

        receivedEvents.ShouldNotReceiveAnyItem();

        await dispatcher.DispatchEvent(testEvent3);

        receivedEvents.ShouldReceiveItem(testEvent3);
        receivedEvents.ShouldReceiveItem(testEvent3);

        receivedEvents.ShouldNotReceiveAnyItem();
    }

    public void Dispose()
    {
        receivedEvents.Dispose();
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorEventingWebSocketsControllers(o =>
        {
            // TODO: add logic to set this to custom path for a single test case (same approach as server tests)
            o.EndpointPath = null;
        });
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorEventingWebSocketsClient(o =>
        {
            _ = o.UseWebSocketFactory(async uri =>
            {
                var webSocket = await ConnectToWebSocket(uri.AbsolutePath, uri.Query);
                return webSocket;
            });

            o.JsonSerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
            };

            // TODO: add logic to set this to custom path for a single test case (same approach as server tests)
            o.EndpointPath = null;
        });

        _ = services.AddSingleton(receivedEvents);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddConquerorEventObserverDelegate<TestDelegateEvent>(async (evt, _, ct) =>
                    {
                        await Task.Yield();
                        receivedEvents.Add(evt, ct);
                    });
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [WebSocketsEvent]
    public sealed record TestEvent
    {
        public int Payload { get; init; }
    }

    [WebSocketsEvent]
    public sealed record TestDelegateEvent
    {
        public int Payload { get; init; }
    }

    public sealed record NonWebSocketsTestEvent
    {
        public int Payload { get; init; }
    }

    public interface ITestEventObserver : IEventObserver<TestEvent>
    {
    }

    public interface INonWebSocketsTestEventObserver : IEventObserver<NonWebSocketsTestEvent>
    {
    }

    public sealed class TestEventObserver : ITestEventObserver
    {
        private readonly BlockingCollection<object> receivedEvents;

        public TestEventObserver(BlockingCollection<object> receivedEvents)
        {
            this.receivedEvents = receivedEvents;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            receivedEvents.Add(evt, cancellationToken);
        }
    }

    public sealed class TestEventObserver2 : ITestEventObserver
    {
        private readonly BlockingCollection<object> receivedEvents;

        public TestEventObserver2(BlockingCollection<object> receivedEvents)
        {
            this.receivedEvents = receivedEvents;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            receivedEvents.Add(evt, cancellationToken);
        }
    }

    public sealed class NonWebSocketsTestEventObserver : INonWebSocketsTestEventObserver
    {
        public Task HandleEvent(NonWebSocketsTestEvent evt, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ThrowingTestHttpClient : HttpClient
    {
        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException();
        }
    }
}
