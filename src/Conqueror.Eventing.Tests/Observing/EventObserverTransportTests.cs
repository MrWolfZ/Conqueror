using System.Collections.Concurrent;

namespace Conqueror.Eventing.Tests.Observing;

public sealed class EventObserverTransportTests
{
    [Test]
    public async Task GivenEventObserverWithoutTransportConfigurationForEventWithCustomTransport_TransportClientIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, null),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, null),
        }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithoutTransportConfigurationForEventWithCustomTransport_TransportClientIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserverDelegate<TestEventWithCustomTransport>(async (evt, p, _) =>
                    {
                        await Task.Yield();

                        var testObservations = p.GetRequiredService<TestObservations>();

                        testObservations.EventsFromObserver.Enqueue(evt);
                    })
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, null),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, null),
        }));
    }

    [Test]
    public async Task GivenEventObserverWithTransportConfigurationForEventWithCustomTransport_TransportClientIsUsedWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportConfiguration = new CustomEventObserverTransport1Configuration { Parameter = 50 };
        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserver>(configureTransports: builder => builder.AddOrReplaceConfiguration(transportConfiguration))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithTransportConfigurationForEventWithCustomTransport_TransportClientIsUsedWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportConfiguration = new CustomEventObserverTransport1Configuration { Parameter = 50 };
        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserverDelegate<TestEventWithCustomTransport>(async (evt, p, _) =>
                                                                                     {
                                                                                         await Task.Yield();

                                                                                         var testObservations = p.GetRequiredService<TestObservations>();

                                                                                         testObservations.EventsFromObserver.Enqueue(evt);
                                                                                     },
                                                                                     builder => builder.AddOrReplaceConfiguration(transportConfiguration))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.EquivalentTo(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.EquivalentTo(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));
    }

    [Test]
    public async Task GivenEventObserverWithPipelineForEventWithCustomTransport_TransportClientIsUsedWithConfigurationAndMiddlewaresAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportConfiguration = new CustomEventObserverTransport1Configuration { Parameter = 50 };
        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserverWithMiddleware>(configureTransports: builder => builder.AddOrReplaceConfiguration(transportConfiguration))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.EquivalentTo(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.EquivalentTo(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithPipelineForEventWithCustomTransport_TransportClientIsUsedWithConfigurationAndMiddlewaresAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportConfiguration = new CustomEventObserverTransport1Configuration { Parameter = 50 };
        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserverDelegate<TestEventWithCustomTransport>(async (evt, p, _) =>
                                                                                     {
                                                                                         await Task.Yield();

                                                                                         var testObservations = p.GetRequiredService<TestObservations>();

                                                                                         testObservations.EventsFromObserver.Enqueue(evt);
                                                                                     },
                                                                                     pipeline => pipeline.Use<TestEventObserverMiddleware>(),
                                                                                     builder => builder.AddOrReplaceConfiguration(transportConfiguration))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.EquivalentTo(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.EquivalentTo(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransport), transportAttribute, transportConfiguration),
        }));
    }

    [Test]
    public async Task GivenEventObserverWithoutTransportConfigurationsForEventWithMultipleCustomTransports_TransportClientsAreUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transport1Attribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type
        var transport2Attribute = new CustomEventTransport2Attribute { Parameter = 20 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultipleTransports>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultipleTransports();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EquivalentTo(new[]
        {
            (typeof(CustomEventTransport1Client), testEvent),
            (typeof(CustomEventTransport2Client), testEvent),
        }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithMultipleTransports), transport1Attribute, null),
            (typeof(CustomEventTransport2Client), typeof(TestEventWithMultipleTransports), transport2Attribute, null),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent, testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EquivalentTo(new[]
        {
            (typeof(CustomEventTransport1Client), testEvent),
            (typeof(CustomEventTransport2Client), testEvent),

            (typeof(CustomEventTransport1Client), testEvent),
            (typeof(CustomEventTransport2Client), testEvent),
        }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithMultipleTransports), transport1Attribute, null),
            (typeof(CustomEventTransport2Client), typeof(TestEventWithMultipleTransports), transport2Attribute, null),
        }));
    }

    [Test]
    public async Task GivenEventObserverWithTransportConfigurationsForEventWithMultipleCustomTransports_TransportClientsAreUsedWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transport1Configuration = new CustomEventObserverTransport1Configuration { Parameter = 50 };
        var transport2Configuration = new CustomEventObserverTransport2Configuration { Parameter = 100 };
        var transport1Attribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type
        var transport2Attribute = new CustomEventTransport2Attribute { Parameter = 20 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserver>(configureTransports: builder => builder.AddOrReplaceConfiguration(transport1Configuration)
                                                                                                         .AddOrReplaceConfiguration(transport2Configuration))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultipleTransports>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultipleTransports();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EquivalentTo(new[]
        {
            (typeof(CustomEventTransport1Client), testEvent),
            (typeof(CustomEventTransport2Client), testEvent),
        }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithMultipleTransports), transport1Attribute, transport1Configuration),
            (typeof(CustomEventTransport2Client), typeof(TestEventWithMultipleTransports), transport2Attribute, transport2Configuration),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent, testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EquivalentTo(new[]
        {
            (typeof(CustomEventTransport1Client), testEvent),
            (typeof(CustomEventTransport2Client), testEvent),

            (typeof(CustomEventTransport1Client), testEvent),
            (typeof(CustomEventTransport2Client), testEvent),
        }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithMultipleTransports), transport1Attribute, transport1Configuration),
            (typeof(CustomEventTransport2Client), typeof(TestEventWithMultipleTransports), transport2Attribute, transport2Configuration),
        }));
    }

    [Test]
    public async Task GivenEventObserverWithCustomTransportConfiguration_TransportBuilderIsOnlyCalledOnceEvenIfUsedMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var builderCallCount = 0;

        var transportConfiguration = new CustomEventObserverTransport1Configuration { Parameter = 50 };

        _ = services.AddConquerorEventObserver<TestEventObserver>(configureTransports: builder =>
                    {
                        builderCallCount += 1;
                        _ = builder.AddOrReplaceConfiguration(transportConfiguration);
                    })
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer1.HandleEvent(testEvent);
        await observer2.HandleEvent(testEvent);
        await dispatcher.DispatchEvent(testEvent);

        Assert.That(builderCallCount, Is.EqualTo(1));
    }

    [Test]
    public void GivenEventObserverWithTransportConfigurationCallbackThatThrowsOnce_WhenPublishingViaInMemoryPublisher_OnlyFirstPublishFailsAndSubsequentPublishesSucceed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var hasThrown = false;
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<TestEventObserver>(configureTransports: _ =>
                    {
                        if (!hasThrown)
                        {
                            hasThrown = true;
                            throw exception;
                        }
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithoutCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithoutCustomTransport();

        Assert.That(() => observer.HandleEvent(testEvent), Throws.Exception.SameAs(exception));
        Assert.That(() => observer.HandleEvent(testEvent), Throws.Nothing);
        Assert.That(() => dispatcher.DispatchEvent(testEvent), Throws.Nothing);
    }

    [Test]
    public async Task GivenEventWithCustomTransportAndInMemoryPublishingStrategy_EventsForTransportAreDispatchedViaCustomStrategy()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportConfiguration = new CustomEventObserverTransport1Configuration { Parameter = 50 };

        _ = services.AddConquerorEventObserver<TestEventObserver>(configureTransports: builder => builder.AddOrReplaceConfiguration(transportConfiguration))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromStrategy, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromStrategy, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
    }

    [Test]
    public async Task GivenMultipleEventObserversForEventWithCustomTransport_AllObserversAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), testEvent),
            (typeof(TestEventObserver2), testEvent),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), testEvent),
            (typeof(TestEventObserver2), testEvent),

            (typeof(TestEventObserver), testEvent),
            (typeof(TestEventObserver2), testEvent),
        }));
    }

    [Test]
    public async Task GivenMultipleEventObserversForEventWithCustomTransport_WhenClientFiltersOutObserver_OnlyObserversSpecifiedByClientAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>(configureTransports: builder => builder.AddOrReplaceConfiguration(new CustomEventObserverTransport1Configuration()))
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton<Func<ConquerorEventTransportClientObserverRegistration<CustomEventObserverTransport1Configuration, CustomEventTransport1Attribute>, bool>>(r => r.Configuration is not null)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver2), testEvent),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.EqualTo(new[]
        {
            (typeof(TestEventObserver2), testEvent),
            (typeof(TestEventObserver2), testEvent),
        }));
    }

    [Test]
    public async Task GivenEventObserverForBaseEventTypeWithCustomTransport_WhenClientPublishesSubType_ObserverIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserverForBaseClass>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>(ServiceLifetime.Singleton)
                    .AddSingleton<CustomEventTransport1Client>()
                    .AddSingleton<CustomEventTransport2Client>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Client>().Start();
        await provider.GetRequiredService<CustomEventTransport2Client>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransportSub>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransportSub();

        await observer.HandleEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransportBase), transportAttribute, null),
        }));

        await dispatcher.DispatchEvent(testEvent);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromClient, Is.EqualTo(new[] { (typeof(CustomEventTransport1Client), testEvent), (typeof(CustomEventTransport1Client), testEvent) }));
        Assert.That(observations.ObservedTransportRegistrations, Is.SupersetOf(new (Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)[]
        {
            (typeof(CustomEventTransport1Client), typeof(TestEventWithCustomTransportBase), transportAttribute, null),
        }));
    }

    private sealed record TestEventWithoutCustomTransport;

    [CustomEventTransport1(Parameter = 10)]
    private sealed record TestEventWithCustomTransport;

    [CustomEventTransport1(Parameter = 10)]
    [CustomEventTransport2(Parameter = 20)]
    private sealed record TestEventWithMultipleTransports;

    private sealed record TestEventWithCustomTransportSub : TestEventWithCustomTransportBase;

    [CustomEventTransport1(Parameter = 10)]
    private abstract record TestEventWithCustomTransportBase;

    private sealed class TestEventObserver(TestObservations observations) : IEventObserver<TestEventWithCustomTransport>,
                                                                            IEventObserver<TestEventWithMultipleTransports>
    {
        public async Task HandleEvent(TestEventWithCustomTransport evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Enqueue(evt);
            observations.EventsFromObserverWithObserverType.Enqueue((GetType(), evt));
        }

        public async Task HandleEvent(TestEventWithMultipleTransports evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Enqueue(evt);
            observations.EventsFromObserverWithObserverType.Enqueue((GetType(), evt));
        }
    }

    private sealed class TestEventObserver2(TestObservations observations) : IEventObserver<TestEventWithCustomTransport>
    {
        public async Task HandleEvent(TestEventWithCustomTransport evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Enqueue(evt);
            observations.EventsFromObserverWithObserverType.Enqueue((GetType(), evt));
        }
    }

    private sealed class TestEventObserverForBaseClass(TestObservations observations) : IEventObserver<TestEventWithCustomTransportBase>
    {
        public async Task HandleEvent(TestEventWithCustomTransportBase evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Enqueue(evt);
            observations.EventsFromObserverWithObserverType.Enqueue((GetType(), evt));
        }
    }

    private sealed class TestEventObserverWithMiddleware(TestObservations observations) : IEventObserver<TestEventWithCustomTransport>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEventWithCustomTransport evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Enqueue(evt);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware>();
        }
    }

    private sealed class TestEventObserverMiddleware(TestObservations observations) : IEventObserverMiddleware
    {
        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();

            observations.EventsFromMiddleware.Enqueue(ctx.Event);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class CustomEventTransport1Attribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; init; }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || (obj is CustomEventTransport1Attribute other && Equals(other));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Parameter);
        }

        private bool Equals(CustomEventTransport1Attribute other)
        {
            return base.Equals(other) && Parameter == other.Parameter;
        }
    }

    private delegate void CustomEventTransport1EventHandler(object evt, ManualResetEventSlim resetEvent);

    private sealed class CustomEventTransport1Publisher : IConquerorEventTransportPublisher<CustomEventTransport1Attribute>
    {
        public event CustomEventTransport1EventHandler? OnEvent;

        public async Task PublishEvent<TEvent>(TEvent evt, CustomEventTransport1Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            using var resetEvent = new ManualResetEventSlim();

            OnEvent?.Invoke(evt, resetEvent);

            resetEvent.Wait(cancellationToken);
            resetEvent.Reset();
        }
    }

    private sealed class CustomEventObserverTransport1Configuration : IEventObserverTransportConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class CustomEventTransport1Client(
        TestObservations observations,
        CustomEventTransport1Publisher publisher,
        IConquerorEventTransportClientRegistrar transportClientRegistrar,
        IServiceProvider serviceProvider,
        Func<ConquerorEventTransportClientObserverRegistration<CustomEventObserverTransport1Configuration, CustomEventTransport1Attribute>, bool>? observerFilter = null)
    {
        public async Task Start()
        {
            var registration = await transportClientRegistrar.RegisterTransportClient<CustomEventObserverTransport1Configuration, CustomEventTransport1Attribute>(builder =>
            {
                // test publishing with custom strategy
                _ = builder.UseDefault(new CustomInMemoryPublishingStrategy(observations));
            });

            foreach (var transportClientRegistration in registration.RelevantObservers)
            {
                observations.ObservedTransportRegistrations.Enqueue((GetType(),
                                                                         transportClientRegistration.EventType,
                                                                         transportClientRegistration.ConfigurationAttribute,
                                                                         transportClientRegistration.Configuration));
            }

            var observersToDispatchTo = registration.RelevantObservers.Where(r => observerFilter?.Invoke(r) ?? true).Select(r => r.ObserverId).ToHashSet();

            publisher.OnEvent += async (evt, resetEvent) =>
            {
                observations.EventsFromClient.Enqueue((GetType(), evt));
                await registration.Dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider);
                resetEvent.Set();
            };
        }
    }

    private sealed class CustomInMemoryPublishingStrategy(TestObservations observations) : IConquerorInMemoryEventPublishingStrategy
    {
        public async Task PublishEvent<TEvent>(IReadOnlyCollection<IEventObserver<TEvent>> eventObservers, TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            observations.EventsFromStrategy.Enqueue(evt);

            foreach (var observer in eventObservers)
            {
                await observer.HandleEvent(evt, cancellationToken);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class CustomEventTransport2Attribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public int Parameter { get; init; }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || (obj is CustomEventTransport2Attribute other && Equals(other));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Parameter);
        }

        private bool Equals(CustomEventTransport2Attribute other)
        {
            return base.Equals(other) && Parameter == other.Parameter;
        }
    }

    private delegate void CustomEventTransport2EventHandler(object evt, ManualResetEventSlim resetEvent);

    private sealed class CustomEventTransport2Publisher : IConquerorEventTransportPublisher<CustomEventTransport2Attribute>
    {
        public event CustomEventTransport2EventHandler? OnEvent;

        public async Task PublishEvent<TEvent>(TEvent evt, CustomEventTransport2Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            using var resetEvent = new ManualResetEventSlim();

            OnEvent?.Invoke(evt, resetEvent);

            resetEvent.Wait(cancellationToken);
            resetEvent.Reset();
        }
    }

    private sealed class CustomEventObserverTransport2Configuration : IEventObserverTransportConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class CustomEventTransport2Client(
        TestObservations observations,
        CustomEventTransport2Publisher publisher,
        IConquerorEventTransportClientRegistrar transportClientRegistrar,
        IServiceProvider serviceProvider)
    {
        public async Task Start()
        {
            var registration = await transportClientRegistrar.RegisterTransportClient<CustomEventObserverTransport2Configuration, CustomEventTransport2Attribute>(builder => builder.UseSequentialAsDefault())
                                                             .ConfigureAwait(false);

            foreach (var transportClientRegistration in registration.RelevantObservers)
            {
                observations.ObservedTransportRegistrations.Enqueue((GetType(),
                                                                         transportClientRegistration.EventType,
                                                                         transportClientRegistration.ConfigurationAttribute,
                                                                         transportClientRegistration.Configuration));
            }

            var observersToDispatchTo = registration.RelevantObservers.Select(r => r.ObserverId).ToHashSet();

            publisher.OnEvent += async (evt, resetEvent) =>
            {
                observations.EventsFromClient.Enqueue((GetType(), evt));
                await registration.Dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider);
                resetEvent.Set();
            };
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<(Type ClientType, Type EventType, object ConfigurationAttribute, object? Configuration)> ObservedTransportRegistrations { get; } = new();

        public ConcurrentQueue<object> EventsFromObserver { get; } = new();

        public ConcurrentQueue<(Type ObserverType, object Event)> EventsFromObserverWithObserverType { get; } = new();

        public ConcurrentQueue<(Type ClientType, object Event)> EventsFromClient { get; } = new();

        public ConcurrentQueue<object> EventsFromMiddleware { get; } = new();

        public ConcurrentQueue<object> EventsFromStrategy { get; } = new();
    }
}
