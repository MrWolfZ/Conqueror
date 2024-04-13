namespace Conqueror.Eventing.Tests;

public sealed class EventMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenObserverWithNoObserverMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenObserverWithSingleAppliedObserverMiddleware_MiddlewareIsCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware) }));
    }

    [Test]
    public async Task GivenObserverWithSingleAppliedObserverMiddlewareWithParameter_MiddlewareIsCalledWithParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddlewareWithParameter>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EqualTo(new[]
        {
            new TestEventObserverMiddlewareConfiguration { Parameter = 10 },
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EqualTo(new[]
        {
            new TestEventObserverMiddlewareConfiguration { Parameter = 10 },
            new TestEventObserverMiddlewareConfiguration { Parameter = 10 },
        }));
    }

    [Test]
    public async Task GivenObserverWithMultipleAppliedObserverMiddlewares_MiddlewaresAreCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware2) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithSinglePublisherMiddleware_MiddlewareIsCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventPublisherMiddleware) }));
    }

    [Test]
    public async Task GivenEventTypeWithMultiplePublishersWithDifferentPipelines_EachPublishersMiddlewareIsCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(configurePipeline: pipeline => pipeline.Use<TestEventPublisherMiddleware2>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultipleCustomPublishers>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEventWithMultipleCustomPublishers { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[]
        {
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[]
        {
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),

            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithMultiplePublisherMiddlewares_MiddlewaresAreCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new())
                                                                            .Use<TestEventPublisherMiddleware2>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventPublisherMiddleware2) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),

            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
        }));
    }

    [Test]
    public async Task GivenEventWithoutObserverAndSinglePublisherMiddleware_MiddlewareIsCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventPublisherMiddleware) }));
    }

    [Test]
    public async Task GivenMultipleObserversWithDifferentAppliedObserverMiddlewares_MiddlewaresAreCalledForEachObserverRespectively()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddleware>()
                    .AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithSingleAppliedObserverMiddlewareAndSinglePublisherMiddleware_MiddlewaresAreCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventObserverMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverMiddleware),

            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverMiddleware),
        }));
    }

    [Test]
    public async Task GivenMultipleObserversWithDifferentAppliedObserverMiddlewaresAndSinglePublisherMiddleware_MiddlewaresAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddleware>()
                    .AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt, evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithEventMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestEventObserverMiddleware2>()
                                    .Use<TestEventObserverMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware2), typeof(TestEventObserverMiddleware2) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new())
                                    .Use<TestEventObserverMiddleware2>()
                                    .Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new())
                                    .Without<TestEventObserverMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),

            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware),
        }));
    }

    [Test]
    public async Task GivenObserverWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestEventObserverMiddleware2>()
                                    .Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new())
                                    .Use<TestEventObserverMiddleware2>()
                                    .Without<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware2), typeof(TestEventObserverMiddleware2) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithRetryMiddleware_MiddlewaresAreCalledMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt, evt, evt, evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenPublisherRetryMiddleware_MiddlewaresAreCalledMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new())
                                                                            .Use<TestEventPublisherMiddleware2>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherRetryMiddleware),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt, evt, evt, evt, evt, evt, evt, evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherRetryMiddleware),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),

            typeof(TestEventPublisherRetryMiddleware),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventPublisherMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithRetryMiddlewareAndPublisherRetryMiddleware_MiddlewaresAreCalledMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherRetryMiddleware),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[]
        {
            typeof(TestEventPublisherRetryMiddleware),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),

            typeof(TestEventPublisherRetryMiddleware),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventPublisherMiddleware),
            typeof(TestEventObserverRetryMiddleware),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
            typeof(TestEventObserverMiddleware),
            typeof(TestEventObserverMiddleware2),
        }));
    }

    [Test]
    public async Task GivenObserverWithPipelineConfigurationMethodWithoutPipelineConfigurationInterface_MiddlewaresAreNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithPipelineConfigurationWithoutPipelineConfigurationInterface>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.MiddlewareTypes, Is.Empty);

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenObserverAndPublisherMiddlewares_MiddlewaresCanChangeTheEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMutatingMiddlewares>()
                    .AddConquerorEventObserverMiddleware<MutatingTestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<MutatingTestEventObserverMiddleware2>()
                    .AddConquerorEventPublisherMiddleware<MutatingTestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<MutatingTestEventPublisherMiddleware2>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: pipeline => pipeline.Use<MutatingTestEventPublisherMiddleware>()
                                                                                                                             .Use<MutatingTestEventPublisherMiddleware2>())
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt1 = new TestEventWithCustomPublisher { Payload = 0 };
        var evt2 = new TestEventWithCustomPublisher { Payload = 1 };
        var evt3 = new TestEventWithCustomPublisher { Payload = 3 };
        var evt4 = new TestEventWithCustomPublisher { Payload = 7 };
        var evt5 = new TestEventWithCustomPublisher { Payload = 15 };

        await observer.HandleEvent(new() { Payload = 0 });

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt1, evt2, evt3, evt4 }));
        Assert.That(observations.EventsFromObservers, Is.EqualTo(new[] { evt5 }));
        Assert.That(observations.EventsFromPublisher, Is.EqualTo(new[] { evt3 }));

        await dispatcher.DispatchEvent(new TestEventWithCustomPublisher { Payload = 0 });

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt1, evt2, evt3, evt4, evt1, evt2, evt3, evt4 }));
        Assert.That(observations.EventsFromObservers, Is.EqualTo(new[] { evt5, evt5 }));
        Assert.That(observations.EventsFromPublisher, Is.EqualTo(new[] { evt3, evt3 }));
    }

    [Test]
    public async Task GivenCancellationToken_MiddlewaresAndPublisherReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        using var tokenSource = new CancellationTokenSource();

        await observer.HandleEvent(new() { Payload = 10 }, tokenSource.Token);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(new[] { tokenSource.Token, tokenSource.Token }));
        Assert.That(observations.CancellationTokensFromPublisher, Is.EqualTo(new[] { tokenSource.Token }));

        await dispatcher.DispatchEvent(new TestEventWithCustomPublisher { Payload = 10 }, tokenSource.Token);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(new[] { tokenSource.Token, tokenSource.Token, tokenSource.Token, tokenSource.Token }));
        Assert.That(observations.CancellationTokensFromPublisher, Is.EqualTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenObserverAndPublisherMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMutatingMiddlewares>()
                    .AddConquerorEventObserverMiddleware<MutatingTestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<MutatingTestEventObserverMiddleware2>()
                    .AddConquerorEventPublisherMiddleware<MutatingTestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<MutatingTestEventPublisherMiddleware2>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher>(configurePipeline: pipeline => pipeline.Use<MutatingTestEventPublisherMiddleware>()
                                                                                                                             .Use<MutatingTestEventPublisherMiddleware2>())
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        await observer.HandleEvent(new() { Payload = 10 }, tokens.CancellationTokens[0]);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(4)));
        Assert.That(observations.CancellationTokensFromObservers, Is.EqualTo(new[] { tokens.CancellationTokens[4] }));
        Assert.That(observations.CancellationTokensFromPublisher, Is.EqualTo(new[] { tokens.CancellationTokens[2] }));

        await dispatcher.DispatchEvent(new TestEventWithCustomPublisher { Payload = 10 }, tokens.CancellationTokens[0]);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EqualTo(tokens.CancellationTokens.Take(4).Concat(tokens.CancellationTokens.Take(4))));
        Assert.That(observations.CancellationTokensFromObservers, Is.EqualTo(new[] { tokens.CancellationTokens[4], tokens.CancellationTokens[4] }));
        Assert.That(observations.CancellationTokensFromPublisher, Is.EqualTo(new[] { tokens.CancellationTokens[2], tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenObserverPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new() { Payload = 10 });
        await observer2.HandleEvent(new() { Payload = 10 });
        await observer3.HandleEvent(new() { Payload = 10 });

        await dispatcher1.DispatchEvent(new TestEvent { Payload = 10 });
        await dispatcher2.DispatchEvent(new TestEvent { Payload = 10 });
        await dispatcher3.DispatchEvent(new TestEvent { Payload = 10 });

        Assert.That(observedInstances, Has.Count.EqualTo(6));
        Assert.That(observedInstances[0], Is.SameAs(observedInstances[1]).And.SameAs(observedInstances[3]).And.SameAs(observedInstances[4]));
        Assert.That(observedInstances[0], Is.Not.SameAs(observedInstances[2]).And.Not.SameAs(observedInstances[5]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[5]));
    }

    [Test]
    public async Task GivenPublisherPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorInMemoryEventPublisher(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()))
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new() { Payload = 10 });
        await observer2.HandleEvent(new() { Payload = 10 });
        await observer3.HandleEvent(new() { Payload = 10 });

        await dispatcher1.DispatchEvent(new TestEvent { Payload = 10 });
        await dispatcher2.DispatchEvent(new TestEvent { Payload = 10 });
        await dispatcher3.DispatchEvent(new TestEvent { Payload = 10 });

        Assert.That(observedInstances, Has.Count.EqualTo(6));
        Assert.That(observedInstances[0], Is.SameAs(observedInstances[1]).And.SameAs(observedInstances[3]).And.SameAs(observedInstances[4]));
        Assert.That(observedInstances[0], Is.Not.SameAs(observedInstances[2]).And.Not.SameAs(observedInstances[5]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[5]));
    }

    [Test]
    public void GivenObserverPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.Use<TestEventObserverMiddleware2>());

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => observer.HandleEvent(new()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventObserverMiddleware2)));

        exception = Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchEvent(new TestEvent()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventObserverMiddleware2)));
    }

    [Test]
    public void GivenObserverPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new()));

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => observer.HandleEvent(new()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventObserverMiddleware)));

        exception = Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchEvent(new TestEvent()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventObserverMiddleware)));
    }

    [Test]
    public void GivenPublisherPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware2>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => observer.HandleEvent(new()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventPublisherMiddleware2)));

        exception = Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchEvent(new TestEvent()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventPublisherMiddleware2)));
    }

    [Test]
    public void GivenPublisherPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithConfigurableMiddlewares>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => observer.HandleEvent(new()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventPublisherMiddleware)));

        exception = Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchEvent(new TestEvent()));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestEventPublisherMiddleware)));
    }

    [Test]
    public void GivenEventObserverMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserverWithThrowingMiddleware>()
                    .AddConquerorEventObserverMiddleware<ThrowingTestEventObserverMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(new()));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(new TestEvent()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenEventPublisherMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSingleMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventPublisherMiddleware<ThrowingTestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<ThrowingTestEventPublisherMiddleware>())
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(new()));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(new TestEvent()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenObserverDelegateWithSingleAppliedEventObserverMiddleware_MiddlewareIsCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                                                                  {
                                                                      await Task.Yield();
                                                                      var obs = p.GetRequiredService<TestObservations>();
                                                                      obs.EventsFromObservers.Add(evt);
                                                                      obs.CancellationTokensFromObservers.Add(cancellationToken);
                                                                  },
                                                                  pipeline => pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new()))
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventObserverMiddleware), typeof(TestEventObserverMiddleware) }));
    }

    [Test]
    public async Task GivenObserverDelegate_PublisherMiddlewareIsCalledWithEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.EventsFromObservers.Add(evt);
                        obs.CancellationTokensFromObservers.Add(cancellationToken);
                    })
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware) }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsFromMiddlewares, Is.EqualTo(new[] { evt, evt }));
        Assert.That(observations.MiddlewareTypes, Is.EqualTo(new[] { typeof(TestEventPublisherMiddleware), typeof(TestEventPublisherMiddleware) }));
    }

    [Test]
    public void InvalidMiddlewares()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserverMiddleware<TestEventObserverMiddlewareWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserverMiddleware(new TestEventObserverMiddlewareWithMultipleInterfaces()));

        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventPublisherMiddleware<TestEventPublisherMiddlewareWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventPublisherMiddleware(new TestEventPublisherMiddlewareWithMultipleInterfaces()));
    }

    private sealed record TestEvent
    {
        public int Payload { get; init; }
    }

    [TestEventTransport]
    private sealed record TestEventWithCustomPublisher
    {
        public int Payload { get; init; }
    }

    [TestEventTransport]
    [TestEventTransport2]
    private sealed record TestEventWithMultipleCustomPublishers
    {
        public int Payload { get; init; }
    }

    private sealed class TestEventObserverWithSingleMiddleware : IEventObserver<TestEvent>,
                                                                 IEventObserver<TestEventWithCustomPublisher>,
                                                                 IEventObserver<TestEventWithMultipleCustomPublishers>,
                                                                 IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public TestEventObserverWithSingleMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public async Task HandleEvent(TestEventWithCustomPublisher evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public async Task HandleEvent(TestEventWithMultipleCustomPublishers evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new());
        }
    }

    private sealed class TestEventObserverWithSingleMiddlewareWithParameter : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public TestEventObserverWithSingleMiddlewareWithParameter(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestEventObserverWithMultipleMiddlewares : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public TestEventObserverWithMultipleMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new())
                        .Use<TestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverWithConfigurableMiddlewares : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public TestEventObserverWithConfigurableMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            var configurationAction = pipeline.ServiceProvider.GetService<Action<IEventObserverPipelineBuilder>>();
            configurationAction?.Invoke(pipeline);
        }
    }

    private sealed class TestEventObserverWithRetryMiddleware : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public TestEventObserverWithRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverRetryMiddleware>()
                        .Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new())
                        .Use<TestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverWithMultipleMutatingMiddlewares : IEventObserver<TestEvent>, IEventObserver<TestEventWithCustomPublisher>, IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public TestEventObserverWithMultipleMutatingMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public async Task HandleEvent(TestEventWithCustomPublisher evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventsFromObservers.Add(evt);
            observations.CancellationTokensFromObservers.Add(cancellationToken);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<MutatingTestEventObserverMiddleware>()
                        .Use<MutatingTestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverWithPipelineConfigurationWithoutPipelineConfigurationInterface : IEventObserver<TestEvent>
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        // ReSharper disable once UnusedMember.Local
        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestEventObserverWithThrowingMiddleware : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<ThrowingTestEventObserverMiddleware, TestEventObserverMiddlewareConfiguration>(new());
        }
    }

    private sealed record TestEventObserverMiddlewareConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class TestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestEventObserverMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventObserverMiddleware2 : IEventObserverMiddleware
    {
        private readonly TestObservations observations;

        public TestEventObserverMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventObserverRetryMiddleware : IEventObserverMiddleware
    {
        private readonly TestObservations observations;

        public TestEventObserverRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestEventObserverMiddleware : IEventObserverMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestEventObserverMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            if (ctx.Event is TestEvent testEvent)
            {
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 4 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[3]);
            }
            else if (ctx.Event is TestEventWithCustomPublisher testEventWithCustomPublisher)
            {
                var modifiedEvent = testEventWithCustomPublisher with { Payload = testEventWithCustomPublisher.Payload + 4 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[3]);
            }
            else
            {
                await ctx.Next(ctx.Event, cancellationTokensToUse.CancellationTokens[3]);
            }
        }
    }

    private sealed class MutatingTestEventObserverMiddleware2 : IEventObserverMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestEventObserverMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            if (ctx.Event is TestEvent testEvent)
            {
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 8 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[4]);
            }
            else if (ctx.Event is TestEventWithCustomPublisher testEventWithCustomPublisher)
            {
                var modifiedEvent = testEventWithCustomPublisher with { Payload = testEventWithCustomPublisher.Payload + 8 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[4]);
            }
            else
            {
                await ctx.Next(ctx.Event, cancellationTokensToUse.CancellationTokens[4]);
            }
        }
    }

    private sealed class ThrowingTestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        private readonly Exception exception;

        public ThrowingTestEventObserverMiddleware(Exception exception)
        {
            this.exception = exception;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed record TestEventPublisherMiddlewareConfiguration;

    private sealed class TestEventPublisherMiddleware : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestEventPublisherMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventPublisherMiddleware2 : IEventPublisherMiddleware
    {
        private readonly TestObservations observations;

        public TestEventPublisherMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventPublisherRetryMiddleware : IEventPublisherMiddleware
    {
        private readonly TestObservations observations;

        public TestEventPublisherRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestEventPublisherMiddleware : IEventPublisherMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestEventPublisherMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            if (ctx.Event is TestEvent testEvent)
            {
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 1 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[1]);
            }
            else if (ctx.Event is TestEventWithCustomPublisher testEventWithCustomPublisher)
            {
                var modifiedEvent = testEventWithCustomPublisher with { Payload = testEventWithCustomPublisher.Payload + 1 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[1]);
            }
            else
            {
                await ctx.Next(ctx.Event, cancellationTokensToUse.CancellationTokens[1]);
            }
        }
    }

    private sealed class MutatingTestEventPublisherMiddleware2 : IEventPublisherMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestEventPublisherMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.EventsFromMiddlewares.Add(ctx.Event);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            if (ctx.Event is TestEvent testEvent)
            {
                var modifiedEvent = testEvent with { Payload = testEvent.Payload + 2 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[2]);
            }
            else if (ctx.Event is TestEventWithCustomPublisher testEventWithCustomPublisher)
            {
                var modifiedEvent = testEventWithCustomPublisher with { Payload = testEventWithCustomPublisher.Payload + 2 };

                await ctx.Next((TEvent)(object)modifiedEvent, cancellationTokensToUse.CancellationTokens[2]);
            }
            else
            {
                await ctx.Next(ctx.Event, cancellationTokensToUse.CancellationTokens[2]);
            }
        }
    }

    private sealed class ThrowingTestEventPublisherMiddleware : IEventPublisherMiddleware
    {
        private readonly Exception exception;

        public ThrowingTestEventPublisherMiddleware(Exception exception)
        {
            this.exception = exception;
        }

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestEventObserverMiddlewareWithMultipleInterfaces : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>,
                                                                             IEventObserverMiddleware
    {
        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class =>
            throw new InvalidOperationException("this middleware should never be called");

        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class =>
            throw new InvalidOperationException("this middleware should never be called");
    }

    private sealed class TestEventPublisherMiddlewareWithMultipleInterfaces : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>,
                                                                              IEventPublisherMiddleware
    {
        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class =>
            throw new InvalidOperationException("this middleware should never be called");

        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class =>
            throw new InvalidOperationException("this middleware should never be called");
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
    }

    private sealed class TestEventTransportPublisher : IConquerorEventTransportPublisher<TestEventTransportAttribute>
    {
        private readonly TestObservations observations;
        private readonly IConquerorEventTransportClientRegistrar registrar;

        public TestEventTransportPublisher(TestObservations observations, IConquerorEventTransportClientRegistrar registrar)
        {
            this.observations = observations;
            this.registrar = registrar;
        }

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();
            observations.EventsFromPublisher.Add(evt);
            observations.CancellationTokensFromPublisher.Add(cancellationToken);

            var registration = await registrar.RegisterTransportClient<InMemoryEventObserverTransportConfiguration, TestEventTransportAttribute>(builder => builder.UseSequentialAsDefault())
                                              .ConfigureAwait(false);

            var observersToDispatchTo = registration.RelevantObservers.Select(r => r.ObserverId).ToHashSet();
            await registration.Dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider, cancellationToken);
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransport2Attribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
    }

    private sealed class TestEventTransportPublisher2 : IConquerorEventTransportPublisher<TestEventTransport2Attribute>
    {
        private readonly InMemoryEventPublisher inMemoryPublisher;
        private readonly TestObservations observations;

        public TestEventTransportPublisher2(TestObservations observations, InMemoryEventPublisher inMemoryPublisher)
        {
            this.observations = observations;
            this.inMemoryPublisher = inMemoryPublisher;
        }

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventTransport2Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();
            observations.EventsFromPublisher.Add(evt);
            observations.CancellationTokensFromPublisher.Add(cancellationToken);

            await inMemoryPublisher.PublishEvent(evt, new(), serviceProvider, cancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = new();

        public List<object> EventsFromObservers { get; } = new();

        public List<object> EventsFromMiddlewares { get; } = new();

        public List<object> EventsFromPublisher { get; } = new();

        public List<CancellationToken> CancellationTokensFromObservers { get; } = new();

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

        public List<CancellationToken> CancellationTokensFromPublisher { get; } = new();

        public List<object> ConfigurationFromMiddlewares { get; } = new();
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = new();
    }

    private sealed class TestService
    {
    }
}
