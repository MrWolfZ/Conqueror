namespace Conqueror.Eventing.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class EventObserverCustomInterfaceTests
{
    [Test]
    public void GivenObserverWithCustomInterface_ObserverCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IEventObserver<TestEvent>>());
    }

    [Test]
    public void GivenObserverWithCustomInterface_ObserverCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestEventObserver>());
    }

    [Test]
    public async Task GivenObserverWithCustomInterface_ResolvingObserverViaPlainAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var plainInterfaceObserver = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var customInterfaceObserver = provider.GetRequiredService<ITestEventObserver>();

        await plainInterfaceObserver.HandleEvent(new(), CancellationToken.None);
        await customInterfaceObserver.HandleEvent(new(), CancellationToken.None);

        Assert.AreEqual(2, observations.Instances.Count);
        Assert.AreSame(observations.Instances[0], observations.Instances[1]);
    }

    [Test]
    public async Task GivenObserverWithMultipleCustomInterfaces_ResolvingObserverViaEitherInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserverWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var observer1 = provider.GetRequiredService<ITestEventObserver>();
        var observer2 = provider.GetRequiredService<ITestEventObserver2>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);

        Assert.AreEqual(2, observations.Instances.Count);
        Assert.AreSame(observations.Instances[0], observations.Instances[1]);
    }

    [Test]
    public async Task GivenObserverWithMixedCustomAndPlainInterfaces_ResolvingObserverViaEitherInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserverWithMultipleMixedInterfaces>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var observer1 = provider.GetRequiredService<ITestEventObserver>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);

        Assert.AreEqual(2, observations.Instances.Count);
        Assert.AreSame(observations.Instances[0], observations.Instances[1]);
    }

    [Test]
    public async Task GivenObserverWithCustomInterface_ResolvingObserverViaPublisherAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var publisher = provider.GetRequiredService<IEventPublisher>();
        var observer = provider.GetRequiredService<ITestEventObserver>();

        await publisher.PublishEvent(new TestEvent(), CancellationToken.None);
        await observer.HandleEvent(new(), CancellationToken.None);

        Assert.AreEqual(2, observations.Instances.Count);
        Assert.AreSame(observations.Instances[0], observations.Instances[1]);
    }

    [Test]
    public void GivenObserverWithCustomInterfaceWithExtraMethods_RegisteringObserverThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorEventing().AddTransient<TestEventObserverWithCustomInterfaceWithExtraMethod>().FinalizeConquerorRegistrations());
    }

    public sealed record TestEvent;

    public sealed record TestEvent2;

    public interface ITestEventObserver : IEventObserver<TestEvent>
    {
    }

    public interface ITestEventObserver2 : IEventObserver<TestEvent2>
    {
    }

    public interface ITestEventObserverWithExtraMethod : IEventObserver<TestEvent>
    {
        void ExtraMethod();
    }

    private sealed class TestEventObserver : ITestEventObserver
    {
        private readonly TestObservations observations;

        public TestEventObserver(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
        }
    }

    private sealed class TestEventObserverWithMultipleInterfaces : ITestEventObserver, ITestEventObserver2
    {
        private readonly TestObservations observations;

        public TestEventObserverWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
        }
    }

    private sealed class TestEventObserverWithMultipleMixedInterfaces : ITestEventObserver, IEventObserver<TestEvent2>
    {
        private readonly TestObservations observations;

        public TestEventObserverWithMultipleMixedInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
            observations.Instances.Add(this);
        }
    }

    private sealed class TestEventObserverWithCustomInterfaceWithExtraMethod : ITestEventObserverWithExtraMethod
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<IEventObserver<TestEvent>> Instances { get; } = new();
    }
}
