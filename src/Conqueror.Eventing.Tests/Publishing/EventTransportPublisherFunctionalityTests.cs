namespace Conqueror.Eventing.Tests.Publishing;

public sealed class EventTransportPublisherFunctionalityTests
{
    [Test]
    public async Task GivenEventWithPayload_PublisherReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenGenericEventWithPayload_PublisherReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<GenericTestEventObserver<string>>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<GenericTestEvent<string>>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new GenericTestEvent<string>("test event");

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenCancellationToken_PublisherReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        using var tokenSource = new CancellationTokenSource();

        await observer.HandleEvent(new() { Payload = 2 }, tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { tokenSource.Token }));

        await dispatcher.DispatchEvent(new TestEvent { Payload = 2 }, tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_PublisherReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        await observer.HandleEvent(new() { Payload = 2 });

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { default(CancellationToken) }));

        await dispatcher.DispatchEvent(new TestEvent { Payload = 2 });

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { default(CancellationToken), default(CancellationToken) }));
    }

    [Test]
    public void GivenExceptionInPublisher_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations)
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(new TestEvent { Payload = 10 }));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [TestEventTransport(Parameter = 10)]
    private sealed record TestEvent
    {
        public int Payload { get; init; }
    }

    [TestEventTransport(Parameter = 10)]
    private sealed record GenericTestEvent<TPayload>(TPayload Payload);

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    private sealed class GenericTestEventObserver<TPayload> : IEventObserver<GenericTestEvent<TPayload>>
    {
        public async Task HandleEvent(GenericTestEvent<TPayload> evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; set; }
    }

    private sealed class TestEventTransportPublisher(TestObservations observations, Exception? exceptionToThrow = null) : IConquerorEventTransportPublisher<TestEventTransportAttribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            Assert.That(configurationAttribute.Parameter, Is.EqualTo(10));

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestObservations
    {
        public List<object> Events { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
