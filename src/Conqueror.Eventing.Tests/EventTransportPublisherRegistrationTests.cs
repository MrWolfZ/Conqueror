namespace Conqueror.Eventing.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for assembly scanning to work")]
public sealed class EventTransportPublisherRegistrationTests
{
    [Test]
    public async Task GivenRegisteredPublisherWithPipeline_WhenRegisteringSamePublisherAgainWithDifferentPipeline_PipelineIsOverwritten()
    {
        var services = new ServiceCollection();
        var originalPipelineWasCalled = false;
        var newPipelineWasCalled = false;

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: _ => originalPipelineWasCalled = true)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: _ => newPipelineWasCalled = true);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        await observer.HandleEvent(new());

        Assert.That(originalPipelineWasCalled, Is.False);
        Assert.That(newPipelineWasCalled, Is.True);

        originalPipelineWasCalled = false;
        newPipelineWasCalled = false;

        await dispatcher.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(originalPipelineWasCalled, Is.False);
        Assert.That(newPipelineWasCalled, Is.True);
    }

    [Test]
    public void GivenEventPublishersRegisteredViaAssemblyScanning_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorEventingTypesFromExecutingAssembly()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<EventPublisherRegistry>();

        var registrations = registry.GetRelevantPublishersForEventType<TestEventWithCustomPublisher>();

        Assert.That(registrations.Select(r => r.Registration), Contains.Item(new EventPublisherRegistration(typeof(TestEventTransportPublisher), typeof(TestEventTransportAttribute), null)));
    }

    [Test]
    public void GivenObserverWithInvalidInterface_RegisteringObserverThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher(new TestEventTransportPublisherWithoutValidInterfaces()));

        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher(new TestEventTransportPublisherWithMultipleInterfaces()));
    }

    [TestEventTransport(Parameter = 10)]
    public sealed record TestEventWithCustomPublisher;

    public sealed class TestEventObserver : IEventObserver<TestEventWithCustomPublisher>
    {
        public async Task HandleEvent(TestEventWithCustomPublisher evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransportAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransport2Attribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
    }

    public sealed class TestEventTransportPublisher : IConquerorEventTransportPublisher<TestEventTransportAttribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            Assert.That(configurationAttribute.Parameter, Is.EqualTo(10));
        }
    }

    private sealed class TestEventTransportPublisherWithoutValidInterfaces : IConquerorEventTransportPublisher
    {
    }

    private sealed class TestEventTransportPublisherWithMultipleInterfaces : IConquerorEventTransportPublisher<TestEventTransportAttribute>, IConquerorEventTransportPublisher<TestEventTransport2Attribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();
        }

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransport2Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();
        }
    }
}
