using System.Collections.Concurrent;
using Conqueror.Eventing.Publishing;

namespace Conqueror.Eventing.Tests.Publishing;

public sealed class EventTransportPublisherSelectionTests
{
    [Test]
    public async Task GivenEventWithoutCustomPublisher_InMemoryPublisherIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddConquerorEventPublisherMiddleware<InMemoryTestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<InMemoryTestEventPublisherMiddleware>()) // use a middleware to capture the execution of the built-in publisher
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithoutCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithoutCustomPublisher();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(InMemoryEventPublisher), testEvent) }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(InMemoryEventPublisher), testEvent), (typeof(InMemoryEventPublisher), testEvent) }));
    }

    [Test]
    public async Task GivenEventWithCustomPublisher_CorrectPublisherIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomPublisher();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(TestEventTransportPublisher1), testEvent) }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(TestEventTransportPublisher1), testEvent), (typeof(TestEventTransportPublisher1), testEvent) }));
    }

    [Test]
    public async Task GivenEventWithMultiplePublishers_CorrectPublishersAreUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddConquerorEventPublisherMiddleware<InMemoryTestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<InMemoryTestEventPublisherMiddleware>()) // use a middleware to capture the execution of the built-in publisher
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new (Type, object)[]
        {
            (typeof(TestEventTransportPublisher1), testEvent),
            (typeof(TestEventTransportPublisher2), testEvent),
            (typeof(InMemoryEventPublisher), testEvent),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new (Type, object)[]
        {
            (typeof(TestEventTransportPublisher1), testEvent),
            (typeof(TestEventTransportPublisher2), testEvent),
            (typeof(InMemoryEventPublisher), testEvent),

            (typeof(TestEventTransportPublisher1), testEvent),
            (typeof(TestEventTransportPublisher2), testEvent),
            (typeof(InMemoryEventPublisher), testEvent),
        }));
    }

    [Test]
    public void GivenEventWithMultiplePublishers_WhenOnePublisherThrows_PublishThrowsSameException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(_ => new TestEventTransportPublisher1(observations, exception))
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(testEvent));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(testEvent));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenEventWithMultiplePublishers_WhenMultiplePublishersThrow_PublishThrowsAggregateException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var exception1 = new Exception();
        var exception2 = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(_ => new TestEventTransportPublisher1(observations, exception1))
                    .AddConquerorEventTransportPublisher(_ => new TestEventTransportPublisher2(observations, exception2))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        var thrownException = Assert.ThrowsAsync<AggregateException>(() => observer.HandleEvent(testEvent));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception1, exception2 }));

        thrownException = Assert.ThrowsAsync<AggregateException>(() => dispatcher.DispatchEvent(testEvent));

        Assert.That(thrownException?.InnerExceptions, Is.EquivalentTo(new[] { exception1, exception2 }));
    }

    [Test]
    public void GivenEventWithMultiplePublishers_WhenOnePublisherThrows_AllPublishersAreStillExecuted()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(_ => new TestEventTransportPublisher1(observations, new InvalidOperationException()))
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddConquerorEventPublisherMiddleware<InMemoryTestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<InMemoryTestEventPublisherMiddleware>()) // use a middleware to capture the execution of the built-in publisher
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultiplePublishers();

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => observer.HandleEvent(testEvent));

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new (Type, object)[]
        {
            (typeof(TestEventTransportPublisher1), testEvent),
            (typeof(TestEventTransportPublisher2), testEvent),
            (typeof(InMemoryEventPublisher), testEvent),
        }));

        _ = Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchEvent(testEvent));

        Assert.That(observations.ObservedPublishes, Is.EquivalentTo(new (Type, object)[]
        {
            (typeof(TestEventTransportPublisher1), testEvent),
            (typeof(TestEventTransportPublisher2), testEvent),
            (typeof(InMemoryEventPublisher), testEvent),

            (typeof(TestEventTransportPublisher1), testEvent),
            (typeof(TestEventTransportPublisher2), testEvent),
            (typeof(InMemoryEventPublisher), testEvent),
        }));
    }

    [Test]
    public async Task GivenMultipleEventsWithDifferentPublishers_CorrectPublishersAreUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddConquerorEventPublisherMiddleware<InMemoryTestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<InMemoryTestEventPublisherMiddleware>()) // use a middleware to capture the execution of the built-in publisher
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEventWithoutCustomPublisher>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent1 = new TestEventWithoutCustomPublisher();
        var testEvent2 = new TestEventWithCustomPublisher();
        var testEvent3 = new TestEventWithoutCustomPublisher();

        await observer1.HandleEvent(testEvent1);
        await observer2.HandleEvent(testEvent2);
        await observer1.HandleEvent(testEvent3);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new (Type, object)[]
        {
            (typeof(InMemoryEventPublisher), testEvent1),
            (typeof(TestEventTransportPublisher1), testEvent2),
            (typeof(InMemoryEventPublisher), testEvent3),
        }));

        await dispatcher.DispatchEvent(testEvent1);
        await dispatcher.DispatchEvent(testEvent2);
        await dispatcher.DispatchEvent(testEvent3);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new (Type, object)[]
        {
            (typeof(InMemoryEventPublisher), testEvent1),
            (typeof(TestEventTransportPublisher1), testEvent2),
            (typeof(InMemoryEventPublisher), testEvent3),

            (typeof(InMemoryEventPublisher), testEvent1),
            (typeof(TestEventTransportPublisher1), testEvent2),
            (typeof(InMemoryEventPublisher), testEvent3),
        }));
    }

    [Test]
    public void GivenEventWithUnregisteredCustomPublisher_ThrowsUnknownPublisherException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomPublisher();

        var thrownException = Assert.ThrowsAsync<ConquerorUnknownEventTransportPublisherException>(() => observer.HandleEvent(testEvent));

        Assert.That(thrownException, Is.Not.Null);
        Assert.That(thrownException?.Message, Contains.Substring("trying to publish event with unknown publisher"));
        Assert.That(thrownException?.Message, Contains.Substring(nameof(TestEventPublisher1Attribute)));

        thrownException = Assert.ThrowsAsync<ConquerorUnknownEventTransportPublisherException>(() => dispatcher.DispatchEvent(testEvent));

        Assert.That(thrownException, Is.Not.Null);
        Assert.That(thrownException?.Message, Contains.Substring("trying to publish event with unknown publisher"));
        Assert.That(thrownException?.Message, Contains.Substring(nameof(TestEventPublisher1Attribute)));
    }

    [Test]
    public async Task GivenEventWithCustomPublisherAndWithoutObserver_CorrectPublisherIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomPublisher();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(TestEventTransportPublisher1), testEvent) }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.ObservedPublishes, Is.EqualTo(new[] { (typeof(TestEventTransportPublisher1), testEvent), (typeof(TestEventTransportPublisher1), testEvent) }));
    }

    private sealed record TestEventWithoutCustomPublisher;

    [TestEventPublisher1(Parameter = 10)]
    private sealed record TestEventWithCustomPublisher;

    [TestEventPublisher1(Parameter = 10)]
    [TestEventPublisher2(Parameter = 20)]
    [InMemoryEvent]
    private sealed record TestEventWithMultiplePublishers;

    private sealed class TestEventObserver : IEventObserver<TestEventWithoutCustomPublisher>,
                                             IEventObserver<TestEventWithCustomPublisher>,
                                             IEventObserver<TestEventWithMultiplePublishers>
    {
        public async Task HandleEvent(TestEventWithoutCustomPublisher query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public async Task HandleEvent(TestEventWithCustomPublisher evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public async Task HandleEvent(TestEventWithMultiplePublishers evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventPublisher1Attribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; set; }
    }

    private sealed class TestEventTransportPublisher1(TestObservations observations, Exception? exception = null) : IConquerorEventTransportPublisher<TestEventPublisher1Attribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, TestEventPublisher1Attribute configurationAttribute, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            Assert.That(configurationAttribute.Parameter, Is.EqualTo(10));

            observations.ObservedPublishes.Enqueue((GetType(), evt));

            if (exception is not null)
            {
                throw exception;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventPublisher2Attribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; set; }
    }

    private sealed class TestEventTransportPublisher2(TestObservations observations, Exception? exception = null) : IConquerorEventTransportPublisher<TestEventPublisher2Attribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, TestEventPublisher2Attribute configurationAttribute, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            Assert.That(configurationAttribute.Parameter, Is.EqualTo(20));

            observations.ObservedPublishes.Enqueue((GetType(), evt));

            if (exception is not null)
            {
                throw exception;
            }
        }
    }

    private sealed class InMemoryTestEventPublisherMiddleware(TestObservations observations) : IEventPublisherMiddleware
    {
        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();

            observations.ObservedPublishes.Enqueue((typeof(InMemoryEventPublisher), ctx.Event));

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<(Type PublisherType, object Event)> ObservedPublishes { get; } = new();
    }
}
