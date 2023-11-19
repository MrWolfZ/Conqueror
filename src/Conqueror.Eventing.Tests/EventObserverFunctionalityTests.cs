namespace Conqueror.Eventing.Tests;

public sealed class EventObserverFunctionalityTests
{
    [Test]
    public async Task GivenEventWithPayload_ObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
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
    public async Task GivenEventWithPayload_DelegateObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Events.Add(evt);
                        obs.CancellationTokens.Add(cancellationToken);
                    })
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
    public async Task GivenEventWithPayloadWithMultipleObservers_ObserversReceiveEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsWithObserverTypes, Is.EquivalentTo(new[]
        {
            (evt, typeof(TestEventObserver)),
            (evt, typeof(TestEventObserver2)),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsWithObserverTypes, Is.EquivalentTo(new[]
        {
            (evt, typeof(TestEventObserver)),
            (evt, typeof(TestEventObserver2)),

            (evt, typeof(TestEventObserver)),
            (evt, typeof(TestEventObserver2)),
        }));
    }

    [Test]
    public async Task GivenEventWithPayloadWithMultipleDelegateObservers_DelegateObserversReceiveEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Events.Add(evt);
                        obs.EventsWithObserverTypes.Add((evt, typeof(int)));
                        obs.CancellationTokens.Add(cancellationToken);
                    })
                    .AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Events.Add(evt);
                        obs.EventsWithObserverTypes.Add((evt, typeof(double)));
                        obs.CancellationTokens.Add(cancellationToken);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsWithObserverTypes, Is.EquivalentTo(new[]
        {
            (evt, typeof(int)),
            (evt, typeof(double)),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsWithObserverTypes, Is.EquivalentTo(new[]
        {
            (evt, typeof(int)),
            (evt, typeof(double)),

            (evt, typeof(int)),
            (evt, typeof(double)),
        }));
    }

    [Test]
    public async Task GivenEventWithPayloadWithMultipleMixedObservers_AllObserversReceiveEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Events.Add(evt);
                        obs.EventsWithObserverTypes.Add((evt, typeof(int)));
                        obs.CancellationTokens.Add(cancellationToken);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.EventsWithObserverTypes, Is.EquivalentTo(new[]
        {
            (evt, typeof(TestEventObserver)),
            (evt, typeof(int)),
        }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.EventsWithObserverTypes, Is.EquivalentTo(new[]
        {
            (evt, typeof(TestEventObserver)),
            (evt, typeof(int)),

            (evt, typeof(TestEventObserver)),
            (evt, typeof(int)),
        }));
    }

    [Test]
    public async Task GivenGenericEventWithPayload_ObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<GenericTestEventObserver<string>>()
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
    public async Task GivenCancellationToken_ObserverReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
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
    public async Task GivenCancellationToken_DelegateObserverReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Events.Add(evt);
                        obs.CancellationTokens.Add(cancellationToken);
                    })
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
    public async Task GivenNoCancellationToken_ObserverReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
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
    public async Task GivenNoCancellationToken_DelegateObserverReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Events.Add(evt);
                        obs.CancellationTokens.Add(cancellationToken);
                    })
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
    public async Task GivenEventTypeWithoutRegisteredObserver_PublishingEventLeadsToNoop()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        await observer.HandleEvent(new() { Payload = 10 });

        Assert.That(observations.Events, Is.Empty);

        await dispatcher.DispatchEvent(new TestEvent { Payload = 10 });

        Assert.That(observations.Events, Is.Empty);
    }

    [Test]
    public void GivenExceptionInObserver_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<ThrowingTestEventObserver>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenExceptionInDelegateObserver_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserverDelegate<TestEvent>(async (_, _, _) =>
        {
            await Task.Yield();
            throw exception;
        });

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));

        thrownException = Assert.ThrowsAsync<Exception>(() => dispatcher.DispatchEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    private sealed record TestEvent
    {
        public int Payload { get; init; }
    }

    private sealed record GenericTestEvent<TPayload>(TPayload Payload);

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        private readonly TestObservations responses;

        public TestEventObserver(TestObservations responses)
        {
            this.responses = responses;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            responses.Events.Add(evt);
            responses.EventsWithObserverTypes.Add((evt, GetType()));
            responses.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class TestEventObserver2 : IEventObserver<TestEvent>
    {
        private readonly TestObservations responses;

        public TestEventObserver2(TestObservations responses)
        {
            this.responses = responses;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            responses.Events.Add(evt);
            responses.EventsWithObserverTypes.Add((evt, GetType()));
            responses.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class GenericTestEventObserver<TPayload> : IEventObserver<GenericTestEvent<TPayload>>
    {
        private readonly TestObservations responses;

        public GenericTestEventObserver(TestObservations responses)
        {
            this.responses = responses;
        }

        public async Task HandleEvent(GenericTestEvent<TPayload> evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            responses.Events.Add(evt);
            responses.EventsWithObserverTypes.Add((evt, GetType()));
            responses.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class ThrowingTestEventObserver : IEventObserver<TestEvent>
    {
        private readonly Exception exception;

        public ThrowingTestEventObserver(Exception exception)
        {
            this.exception = exception;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestObservations
    {
        public List<object> Events { get; } = new();

        public List<(object Event, Type ObserverType)> EventsWithObserverTypes { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}
