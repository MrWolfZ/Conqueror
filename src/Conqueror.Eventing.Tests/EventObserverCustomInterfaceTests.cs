namespace Conqueror.Eventing.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class EventObserverCustomInterfaceTests
{
    [Test]
    public async Task GivenEventWithPayload_ObserverReceivesEventWhenCalledThroughCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<ITestEventObserver>();

        var evt = new TestEvent { Payload = 10 };

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));
    }

    [Test]
    public async Task GivenGenericEventWithPayload_ObserverReceivesEventWhenCalledThroughCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<GenericTestEventObserver<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IGenericTestEventObserver<string>>();

        var evt = new GenericTestEvent<string>("test event");

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));
    }

    [Test]
    public async Task GivenCancellationToken_ObserverReceivesCancellationTokenWhenCalledThroughCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<ITestEventObserver>();
        using var tokenSource = new CancellationTokenSource();

        await observer.HandleEvent(new() { Payload = 2 }, tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_ObserverReceivesDefaultCancellationTokenWhenCalledThroughCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<ITestEventObserver>();

        await observer.HandleEvent(new() { Payload = 2 });

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public void GivenExceptionInObserver_InvocationThrowsSameExceptionWhenCalledThroughCustomInterface()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorEventObserver<ThrowingTestEventObserver>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IThrowingTestEventObserver>();

        var evt = new TestEvent { Payload = 10 };

        var thrownException = Assert.ThrowsAsync<Exception>(() => observer.HandleEvent(evt));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenObserverWithCustomInterface_ObserverCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IEventObserver<TestEvent>>());
    }

    [Test]
    public void GivenObserverWithCustomInterface_ObserverCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestEventObserver>());
    }

    [Test]
    public async Task GivenSingletonObserverWithCustomInterface_ResolvingObserverViaPlainAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var plainInterfaceObserver = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var customInterfaceObserver = provider.GetRequiredService<ITestEventObserver>();

        await plainInterfaceObserver.HandleEvent(new());
        await customInterfaceObserver.HandleEvent(new());

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public async Task GivenSingletonObserverWithMultipleCustomInterfaces_ResolvingObserverViaEitherInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleInterfaces>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<ITestEventObserver>();
        var observer2 = provider.GetRequiredService<ITestEventObserver2>();

        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public async Task GivenSingletonObserverWithMixedCustomAndPlainInterfaces_ResolvingObserverViaEitherInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMixedInterfaces>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<ITestEventObserver>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();

        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public async Task GivenSingletonObserverWithCustomInterface_ResolvingObserverViaDispatcherAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();
        var observer = provider.GetRequiredService<ITestEventObserver>();

        await dispatcher.DispatchEvent(new TestEvent());
        await observer.HandleEvent(new());

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public void GivenObserverWithCustomInterfaceWithExtraMethods_RegisteringObserverThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventObserver<TestEventObserverWithCustomInterfaceWithExtraMethod>());
    }

    public sealed record TestEvent
    {
        public int Payload { get; init; }
    }

    public sealed record TestEvent2;

    public sealed record GenericTestEvent<TPayload>(TPayload Payload);

    public interface ITestEventObserver : IEventObserver<TestEvent>
    {
    }

    public interface ITestEventObserver2 : IEventObserver<TestEvent2>
    {
    }

    public interface IGenericTestEventObserver<TPayload> : IEventObserver<GenericTestEvent<TPayload>>
    {
    }

    public interface IThrowingTestEventObserver : IEventObserver<TestEvent>
    {
    }

    public interface ITestEventObserverWithExtraMethod : IEventObserver<TestEvent>
    {
        void ExtraMethod();
    }

    private sealed class TestEventObserver(TestObservations observations) : ITestEventObserver
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class GenericTestEventObserver<TPayload>(TestObservations observations) : IGenericTestEventObserver<TPayload>
    {
        public async Task HandleEvent(GenericTestEvent<TPayload> evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class ThrowingTestEventObserver(Exception exception) : IThrowingTestEventObserver
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestEventObserverWithMultipleInterfaces(TestObservations observations) : ITestEventObserver, ITestEventObserver2
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class TestEventObserverWithMultipleMixedInterfaces(TestObservations observations) : ITestEventObserver, IEventObserver<TestEvent2>
    {
        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class TestEventObserverWithCustomInterfaceWithExtraMethod : ITestEventObserverWithExtraMethod
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = [];

        public List<object> Events { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
