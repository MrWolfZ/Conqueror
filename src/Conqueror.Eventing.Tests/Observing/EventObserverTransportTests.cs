namespace Conqueror.Eventing.Tests.Observing;

public sealed class EventObserverTransportTests
{
    [Test]
    public async Task GivenEventTypeWithCustomTransport_WhenTransportReceiverBroadcastsEvent_ItIsReceivedByObservers()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSingleton<TestTransportReceiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestTransportReceiver>();

        var evt = new TestEvent(10);

        await receiver.Handle(evt, CancellationToken.None);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransport_WhenTransportReceiverBroadcastsEvent_ObserversReceiveCorrectCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSingleton<TestTransportReceiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestTransportReceiver>();

        using var cts = new CancellationTokenSource();

        var evt = new TestEvent(10);

        await receiver.Handle(evt, cts.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransport_WhenTransportReceiverBroadcastsEvent_ObserversAreResolvedFromReceiverScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddTransient<TestTransportReceiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var receiver1 = scope1.ServiceProvider.GetRequiredService<TestTransportReceiver>();
        var receiver2 = scope2.ServiceProvider.GetRequiredService<TestTransportReceiver>();

        var evt = new TestEvent(10);

        await receiver1.Handle(evt, CancellationToken.None);
        await receiver2.Handle(evt, CancellationToken.None);

        Assert.That(observations.ServiceProviders, Is.EqualTo(new[]
        {
            scope1.ServiceProvider,
            scope1.ServiceProvider,
            scope2.ServiceProvider,
            scope2.ServiceProvider,
        }));
    }

    [TestEventTransport]
    private sealed record TestEvent(int Payload);

    private sealed class TestEventObserver(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestEventObserver2(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute() : EventTransportAttribute(nameof(TestEventTransportAttribute));

    private sealed class TestTransportReceiver(IServiceProvider serviceProvider, IEventTransportRegistry registry, IEventTransportReceiverBroadcaster broadcaster)
    {
        public async Task Handle<TEvent>(TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            var relevantEventTypes = registry.GetEventTypesForReceiver<TestEventTransportAttribute>().Select(t => t.EventType);

            if (relevantEventTypes.Contains(evt.GetType()))
            {
                await broadcaster.Broadcast(evt, new TestEventTransportAttribute(), serviceProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class TestObservations
    {
        public List<object> Events { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];
    }
}
