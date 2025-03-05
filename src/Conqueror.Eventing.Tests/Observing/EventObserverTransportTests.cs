using System.Collections.Concurrent;
using System.Diagnostics;

namespace Conqueror.Eventing.Tests.Observing;

public sealed class EventObserverTransportTests : IDisposable
{
    private readonly CancellationTokenSource timeoutCancellationTokenSource = new();

    public EventObserverTransportTests()
    {
        if (!Debugger.IsAttached)
        {
            timeoutCancellationTokenSource.CancelAfter(TestTimeout);
        }
    }

    private static TimeSpan TestTimeout => TimeSpan.FromSeconds(Environment.GetEnvironmentVariable("GITHUB_ACTION") is null ? 1 : 10);

    private CancellationToken TestTimeoutToken => timeoutCancellationTokenSource.Token;

    [Test]
    public async Task GivenEventObserverForEventWithCustomTransport_TransportClientIsUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[] { (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute) }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
        }));
    }

    [Test]
    public async Task GivenEventObserverDelegateForEventWithCustomTransport_TransportClientIsUsed()
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
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[] { (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute) }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
        }));
    }

    [Test]
    public async Task GivenEventObserverWithPipelineForEventWithCustomTransport_TransportClientIsUsedAndMiddlewaresAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserverWithMiddleware>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[] { (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute) }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
        }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithPipelineForEventWithCustomTransport_TransportClientIsUsedAndMiddlewaresAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserverDelegate<TestEventWithCustomTransport>(async (evt, p, _) =>
                                                                                     {
                                                                                         await Task.Yield();

                                                                                         var testObservations = p.GetRequiredService<TestObservations>();

                                                                                         testObservations.EventsFromObserver.Enqueue(evt);
                                                                                     },
                                                                                     pipeline => pipeline.Use<TestEventObserverMiddleware>())
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[] { (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute) }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromMiddleware, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
        }));
    }

    [Test]
    public async Task GivenEventObserverForEventWithMultipleCustomTransports_TransportClientsAreUsed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transport1Attribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type
        var transport2Attribute = new CustomEventTransport2Attribute { Parameter = 20 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithMultipleTransports>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithMultipleTransports();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EquivalentTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, (object)transport1Attribute),
            (typeof(CustomEventTransport2Receiver), testEvent, transport2Attribute),
        }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent, testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EquivalentTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, (object)transport1Attribute),
            (typeof(CustomEventTransport2Receiver), testEvent, transport2Attribute),

            (typeof(CustomEventTransport1Receiver), testEvent, transport1Attribute),
            (typeof(CustomEventTransport2Receiver), testEvent, transport2Attribute),
        }));
    }

    [Test]
    public async Task GivenMultipleEventObserversForEventWithCustomTransport_AllObserversAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), testEvent),
            (typeof(TestEventObserver2), testEvent),
        }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.EquivalentTo(new[]
        {
            (typeof(TestEventObserver), testEvent),
            (typeof(TestEventObserver2), testEvent),

            (typeof(TestEventObserver), testEvent),
            (typeof(TestEventObserver2), testEvent),
        }));
    }

    [Test]
    public async Task GivenEventWithCustomTransport_WhenReceiverFiltersOutEventType_NoObserversAreCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<Func<Type, bool>>(_ => false)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransport>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransport();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.Empty);

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserverWithObserverType, Is.Empty);
    }

    [Test]
    public async Task GivenEventObserverForBaseEventTypeWithDefaultTransport_WhenClientPublishesSubType_ObserverIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverForBaseClass>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEventSub>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventSub();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
    }

    [Test]
    public async Task GivenEventObserverForBaseEventTypeWithCustomTransport_WhenClientPublishesSubType_ObserverIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var transportAttribute = new CustomEventTransport1Attribute { Parameter = 10 }; // matches annotation on event type

        _ = services.AddConquerorEventObserver<TestEventObserverForBaseClassWithCustomTransport>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport1Publisher>()
                    .AddSingleton<CustomEventTransport1PublisherState>()
                    .AddConquerorEventTransportPublisher<CustomEventTransport2Publisher>()
                    .AddSingleton<CustomEventTransport2PublisherState>()
                    .AddSingleton<CustomEventTransport1Receiver>()
                    .AddSingleton<CustomEventTransport2Receiver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<CustomEventTransport1Receiver>().Start();
        await provider.GetRequiredService<CustomEventTransport2Receiver>().Start();

        var observer = provider.GetRequiredService<IEventObserver<TestEventWithCustomTransportSub>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var testEvent = new TestEventWithCustomTransportSub();

        await observer.HandleEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[] { (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute) }));

        await dispatcher.DispatchEvent(testEvent, TestTimeoutToken);

        Assert.That(observations.EventsFromObserver, Is.EqualTo(new[] { testEvent, testEvent }));
        Assert.That(observations.EventsFromReceiver, Is.EqualTo(new[]
        {
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
            (typeof(CustomEventTransport1Receiver), testEvent, transportAttribute),
        }));
    }

    public void Dispose()
    {
        timeoutCancellationTokenSource.Dispose();
    }

    [CustomEventTransport1(Parameter = 10)]
    private sealed record TestEventWithCustomTransport;

    [CustomEventTransport1(Parameter = 10)]
    [CustomEventTransport2(Parameter = 20)]
    private sealed record TestEventWithMultipleTransports;

    private sealed record TestEventSub : TestEventBase;

    private abstract record TestEventBase;

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

    private sealed class TestEventObserverForBaseClass(TestObservations observations) : IEventObserver<TestEventBase>
    {
        public async Task HandleEvent(TestEventBase evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventsFromObserver.Enqueue(evt);
            observations.EventsFromObserverWithObserverType.Enqueue((GetType(), evt));
        }
    }

    private sealed class TestEventObserverForBaseClassWithCustomTransport(TestObservations observations) : IEventObserver<TestEventWithCustomTransportBase>
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

    private sealed class CustomEventTransport1Publisher(CustomEventTransport1PublisherState state) : IConquerorEventTransportPublisher<CustomEventTransport1Attribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, CustomEventTransport1Attribute configurationAttribute, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            using var resetEvent = new ManualResetEventSlim();

            state.Invoke(evt, resetEvent);

            resetEvent.Wait(cancellationToken);
            resetEvent.Reset();
        }
    }

    private sealed class CustomEventTransport1PublisherState
    {
        public event CustomEventTransport1EventHandler? OnEvent;

        public void Invoke(object evt, ManualResetEventSlim resetEvent)
        {
            OnEvent?.Invoke(evt, resetEvent);
        }
    }

    private sealed class CustomEventTransport1Receiver(
        TestObservations observations,
        CustomEventTransport1PublisherState publisherState,
        IConquerorEventTypeRegistry typeRegistry,
        IConquerorEventTransportReceiverBroadcaster broadcaster,
        IServiceProvider serviceProvider,
        Func<Type, bool>? observerFilter = null)
    {
        public async Task Start()
        {
            await Task.Yield();

            publisherState.OnEvent += async (evt, resetEvent) =>
            {
                if (typeRegistry.TryGetConfigurationForReceiver<CustomEventTransport1Attribute>(evt.GetType(), out var configurationAttribute)
                    && (observerFilter?.Invoke(evt.GetType()) ?? true))
                {
                    observations.EventsFromReceiver.Enqueue((GetType(), evt, configurationAttribute));

                    await broadcaster.Broadcast(evt, serviceProvider, CancellationToken.None);
                }

                resetEvent.Set();
            };
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

    private sealed class CustomEventTransport2Publisher(CustomEventTransport2PublisherState state) : IConquerorEventTransportPublisher<CustomEventTransport2Attribute>
    {
        public async Task PublishEvent<TEvent>(TEvent evt, CustomEventTransport2Attribute configurationAttribute, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            await Task.Yield();

            using var resetEvent = new ManualResetEventSlim();

            state.Invoke(evt, resetEvent);

            resetEvent.Wait(cancellationToken);
            resetEvent.Reset();
        }
    }

    private sealed class CustomEventTransport2PublisherState
    {
        public event CustomEventTransport2EventHandler? OnEvent;

        public void Invoke(object evt, ManualResetEventSlim resetEvent)
        {
            OnEvent?.Invoke(evt, resetEvent);
        }
    }

    private sealed class CustomEventTransport2Receiver(
        TestObservations observations,
        CustomEventTransport2PublisherState publisherState,
        IConquerorEventTypeRegistry typeRegistry,
        IConquerorEventTransportReceiverBroadcaster broadcaster,
        IServiceProvider serviceProvider)
    {
        public async Task Start()
        {
            await Task.Yield();

            publisherState.OnEvent += async (evt, resetEvent) =>
            {
                if (typeRegistry.TryGetConfigurationForReceiver<CustomEventTransport2Attribute>(evt.GetType(), out var configurationAttribute))
                {
                    observations.EventsFromReceiver.Enqueue((GetType(), evt, configurationAttribute));

                    await broadcaster.Broadcast(evt, serviceProvider, CancellationToken.None);
                }

                resetEvent.Set();
            };
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<object> EventsFromObserver { get; } = new();

        public ConcurrentQueue<(Type ObserverType, object Event)> EventsFromObserverWithObserverType { get; } = new();

        public ConcurrentQueue<(Type ReceiverType, object Event, object ConfigurationAttribute)> EventsFromReceiver { get; } = new();

        public ConcurrentQueue<object> EventsFromMiddleware { get; } = new();
    }
}
