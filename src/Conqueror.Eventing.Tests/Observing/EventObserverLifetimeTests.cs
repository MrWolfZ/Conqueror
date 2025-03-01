namespace Conqueror.Eventing.Tests.Observing;

public sealed class EventObserverLifetimeTests
{
    [Test]
    public async Task GivenTransientObserver_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientObserverWithFactory_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver(p => new TestEventObserver(p.GetRequiredService<TestObservations>()))
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedObserver_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Scoped)
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenScopedObserverWithFactory_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver(p => new TestEventObserver(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenSingletonObserver_ResolvingOrExecutingObserverOrDispatcherUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public async Task GivenSingletonObserverWithFactory_ResolvingOrExecutingObserverOrDispatcherUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver(p => new TestEventObserver(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
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

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public async Task GivenMultipleTransientObservers_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>()
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedObservers_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Scoped)
                    .AddConquerorEventObserver<TestEventObserver2>(ServiceLifetime.Scoped)
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 4, 4, 5, 5, 6, 6, 2, 2 }));
    }

    [Test]
    public async Task GivenMultipleSingletonObservers_ResolvingOrExecutingObserverOrDispatcherReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
                    .AddConquerorEventObserver<TestEventObserver2>(ServiceLifetime.Singleton)
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8 }));
    }

    [Test]
    public async Task GivenMultipleObserversWithDifferentLifetimes_ResolvingOrExecutingObserverOrDispatcherReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserver<TestEventObserver2>(ServiceLifetime.Singleton)
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

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1, 8 }));
    }

    [Test]
    public async Task GivenSingletonObserverWithMultipleObserverInterfaces_ResolvingOrExecutingObserverViaEitherInterfaceOrConcreteClassOrExecutingViaDispatcherUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleInterfaces>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();
        var observer3 = provider.GetRequiredService<TestEventObserverWithMultipleInterfaces>();

        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new TestEvent());
        await observer3.HandleEvent(new TestEvent2());

        await dispatcher.DispatchEvent(new TestEvent());
        await dispatcher.DispatchEvent(new TestEvent());
        await dispatcher.DispatchEvent(new TestEvent2());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public async Task GivenSingletonObserverInstance_ResolvingOrExecutingObserverOrDispatcherUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver(new TestEventObserver(observations));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());
        await dispatcher3.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    private sealed record TestEvent;

    private sealed record TestEvent2;

    private sealed class TestEventObserver(TestObservations observations) : IEventObserver<TestEvent>
    {
        private int invocationCount;

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventObserver2(TestObservations observations) : IEventObserver<TestEvent>
    {
        private int invocationCount;

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventObserverWithMultipleInterfaces(TestObservations observations) : IEventObserver<TestEvent>, IEventObserver<TestEvent2>
    {
        private int invocationCount;

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = [];
    }
}
