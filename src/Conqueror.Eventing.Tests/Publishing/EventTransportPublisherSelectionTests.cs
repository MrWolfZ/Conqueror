using System.Collections.Concurrent;

namespace Conqueror.Eventing.Tests.Publishing;

public sealed class EventTransportPublisherSelectionTests
{
    [Test]
    public async Task GivenEventWithCustomTransport_WhenPublishing_CorrectTransportPublisherIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransport2Publisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithCustomPublisher();

        await observer.Handle(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(TestEventTransportPublisher), testEvent) }));

        // assert that in-process publisher is not used by default when a custom transport is used
        Assert.That(observations.EventsFromObserver, Is.Empty);

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransportPublisher), testEvent),
            (typeof(TestEventTransportPublisher), testEvent),
        }));

        Assert.That(observations.EventsFromObserver, Is.Empty);
    }

    [Test]
    public async Task GivenEventWithMultipleCustomTransports_WhenPublishing_CorrectTransportPublishersAreUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransport2Publisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        await observer.Handle(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[]
        {
            (typeof(TestEventTransportPublisher), testEvent),
            (typeof(TestEventTransport2Publisher), testEvent),
        }));

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new[]
        {
            (typeof(TestEventTransportPublisher), testEvent),
            (typeof(TestEventTransport2Publisher), testEvent),
            (typeof(TestEventTransportPublisher), testEvent),
            (typeof(TestEventTransport2Publisher), testEvent),
        }));

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
    }

    [Test]
    public async Task GivenMultipleEventsWithDifferentCustomTransports_WhenPublishing_CorrectTransportPublishersAreUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransport2Publisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent1 = new TestEventWithCustomPublisher();
        var testEvent2 = new TestEventWithCustomPublisher2();

        await observer1.Handle(testEvent1);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(TestEventTransportPublisher), testEvent1) }));

        await observer2.Handle(testEvent2);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransportPublisher), (object)testEvent1),
            (typeof(TestEventTransport2Publisher), testEvent2),
        }));

        await dispatcher.DispatchEvent(testEvent1);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransportPublisher), (object)testEvent1),
            (typeof(TestEventTransport2Publisher), testEvent2),
            (typeof(TestEventTransportPublisher), testEvent1),
        }));

        await dispatcher.DispatchEvent(testEvent2);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransportPublisher), (object)testEvent1),
            (typeof(TestEventTransport2Publisher), testEvent2),
            (typeof(TestEventTransportPublisher), testEvent1),
            (typeof(TestEventTransport2Publisher), testEvent2),
        }));
    }

    [Test]
    public void GivenEventWithMultipleCustomTransports_WhenSinglePublisherThrows_SameExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var exception = new Exception1();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransport2Publisher>()
                    .AddSingleton(new TestObservations())
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        Assert.That(() => observer.Handle(testEvent), Throws.Exception.SameAs(exception));
        Assert.That(() => dispatcher.DispatchEvent(testEvent), Throws.Exception.SameAs(exception));
    }

    [Test]
    public void GivenEventWithMultipleCustomTransports_WhenMultiplePublisherThrow_AggregateExceptionIsThrown()
    {
        var services = new ServiceCollection();
        var exception1 = new Exception1();
        var exception2 = new Exception2();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransport2Publisher>()
                    .AddSingleton(new TestObservations())
                    .AddSingleton(exception1)
                    .AddSingleton(exception2);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        Assert.That(() => observer.Handle(testEvent), Throws.InstanceOf<AggregateException>()
                                                            .With.Property("InnerExceptions").EquivalentTo(new Exception[] { exception1, exception2 }));

        Assert.That(() => dispatcher.DispatchEvent(testEvent), Throws.InstanceOf<AggregateException>()
                                                                     .With.Property("InnerExceptions").EquivalentTo(new Exception[] { exception1, exception2 }));
    }

    [Test]
    public void GivenEventWithMultipleCustomTransports_WhenSinglePublisherThrows_OtherPublishersAreStillExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var exception = new Exception1();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddConquerorEventTransportPublisher<TestEventTransport2Publisher>()
                    .AddSingleton(observations)
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        Assert.That(() => observer.Handle(testEvent), Throws.InstanceOf<Exception1>());

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransport2Publisher), testEvent),
        }));

        Assert.That(() => dispatcher.DispatchEvent(testEvent), Throws.InstanceOf<Exception1>());

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransport2Publisher), testEvent),
            (typeof(TestEventTransport2Publisher), testEvent),
        }));
    }

    [Test]
    public async Task GivenPublisherForMultipleTransports_WhenPublishingEventForEitherTransport_PublisherIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventTransportPublisher<MultiTestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent1 = new TestEventWithCustomPublisher();
        var testEvent2 = new TestEventWithCustomPublisher2();

        await observer1.Handle(testEvent1);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(MultiTestEventTransportPublisher), testEvent1) }));

        await observer2.Handle(testEvent2);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(MultiTestEventTransportPublisher), (object)testEvent1),
            (typeof(MultiTestEventTransportPublisher), testEvent2),
        }));

        await dispatcher.DispatchEvent(testEvent1);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(MultiTestEventTransportPublisher), (object)testEvent1),
            (typeof(MultiTestEventTransportPublisher), testEvent2),
            (typeof(MultiTestEventTransportPublisher), testEvent1),
        }));

        await dispatcher.DispatchEvent(testEvent2);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(MultiTestEventTransportPublisher), (object)testEvent1),
            (typeof(MultiTestEventTransportPublisher), testEvent2),
            (typeof(MultiTestEventTransportPublisher), testEvent1),
            (typeof(MultiTestEventTransportPublisher), testEvent2),
        }));
    }

    [Test]
    public void GivenEventWithMultipleCustomTransports_WhenNoPublisherForAnyTransportIsRegistered_ThrowsUnknownPublisherException()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorEventing().AddSingleton(new TestObservations());

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithCustomPublisher();

        Assert.That(() => observer.Handle(testEvent), Throws.InstanceOf<UnregisteredEventTransportPublisherException>());
        Assert.That(() => dispatcher.DispatchEvent(testEvent), Throws.InstanceOf<UnregisteredEventTransportPublisherException>());
    }

    [Test]
    public async Task GivenEventWithMultipleCustomTransports_WhenAtLeastOnePublisherForAnyTransportIsRegistered_PublishesToTransportWithoutException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        await observer.Handle(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransportPublisher), testEvent),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[]
        {
            (typeof(TestEventTransportPublisher), testEvent),
            (typeof(TestEventTransportPublisher), testEvent),
        }));
    }

    [TestEventTransport]
    private sealed record TestEventWithCustomPublisher;

    [TestEventTransport2]
    private sealed record TestEventWithCustomPublisher2;

    [TestEventTransport]
    [TestEventTransport2]
    [InProcessEvent]
    private sealed record TestEventWithMultiplePublishers;

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute() : EventTransportAttribute(nameof(TestEventTransportAttribute));

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransport2Attribute() : EventTransportAttribute(nameof(TestEventTransport2Attribute));

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransportForAssemblyScanningAttribute() : EventTransportAttribute(nameof(TestEventTransportForAssemblyScanningAttribute));

    private sealed class Exception1 : Exception;

    private sealed class Exception2 : Exception;

    private sealed class TestEventTransportPublisher(TestObservations observations, Exception1? exceptionToThrow = null)
        : IEventTransportPublisher<TestEventTransportAttribute>
    {
        public async Task PublishEvent(object evt, TestEventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.ObservedPublishes.Enqueue((GetType(), evt));
        }
    }

    private sealed class TestEventTransport2Publisher(TestObservations observations, Exception2? exceptionToThrow = null) : IEventTransportPublisher<TestEventTransport2Attribute>
    {
        public async Task PublishEvent(object evt, TestEventTransport2Attribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.ObservedPublishes.Enqueue((GetType(), evt));
        }
    }

    private sealed class MultiTestEventTransportPublisher(TestObservations observations) : IEventTransportPublisher<TestEventTransportAttribute>,
                                                                                           IEventTransportPublisher<TestEventTransport2Attribute>
    {
        public async Task PublishEvent(object evt, TestEventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            observations.ObservedPublishes.Enqueue((GetType(), evt));
        }

        public async Task PublishEvent(object evt, TestEventTransport2Attribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            observations.ObservedPublishes.Enqueue((GetType(), evt));
        }
    }

    private sealed class TestEventObserver(TestObservations observations) : IEventObserver<TestEventWithMultiplePublishers>
    {
        public async Task Handle(TestEventWithMultiplePublishers evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Add(evt);
        }
    }

    private sealed class TestObservations
    {
        public List<object> EventsFromObserver { get; } = new();

        public ConcurrentQueue<(Type PublisherType, object Event)> ObservedPublishes { get; } = new();
    }
}
