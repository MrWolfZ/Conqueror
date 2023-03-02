namespace Conqueror.Eventing.Tests;

public sealed class EventObserverLifetimeTests
{
    [Test]
    public async Task GivenTransientObserver_ResolvingObserverCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedObserver_ResolvingObserverCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddScoped<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonObserver_ResolvingObserverReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenMultipleTransientObservers_ResolvingObserverCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddTransient<TestEventObserver2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedObservers_ResolvingObserverCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddScoped<TestEventObserver>()
                    .AddScoped<TestEventObserver2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleSingletonObservers_ResolvingObserverReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserver>()
                    .AddSingleton<TestEventObserver2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3 }));
    }

    [Test]
    public async Task GivenMultipleObserversWithDifferentLifetimes_ResolvingObserverReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddSingleton<TestEventObserver2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3 }));
    }

    [Test]
    public async Task GivenSingletonObserverWithMultipleObserverInterfaces_ResolvingObserverViaEitherInterfaceReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserverWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonObserver_ResolvingObserverDirectlyAndViaPublisherReturnsSameInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        var publisher = provider.GetRequiredService<IEventPublisher>();
        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new(), CancellationToken.None);
        await publisher.PublishEvent(new TestEvent(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonObserverInstance_ResolvingObserverReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddSingleton(new TestEventObserver(observations));

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);
        await observer3.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    private sealed record TestEvent;

    private sealed record TestEvent2;

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserver(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventObserver2 : IEventObserver<TestEvent>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserver2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventObserverWithMultipleInterfaces : IEventObserver<TestEvent>, IEventObserver<TestEvent2>
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }

        public async Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = new();
    }
}
