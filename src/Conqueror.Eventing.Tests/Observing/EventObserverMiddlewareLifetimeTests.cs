namespace Conqueror.Eventing.Tests.Observing;

public sealed class EventObserverMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
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
    public async Task GivenTransientMiddlewareWithFactory_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware(p => new TestEventObserverMiddleware(p.GetRequiredService<TestObservations>()))
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
    public async Task GivenScopedMiddleware_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Scoped)
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
    public async Task GivenScopedMiddlewareWithFactory_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware(p => new TestEventObserverMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
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
    public async Task GivenSingletonMiddleware_ResolvingOrExecutingObserverOrDispatcherReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Singleton)
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
    public async Task GivenSingletonMiddlewareWithFactory_ResolvingOrExecutingObserverOrDispatcherReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware(p => new TestEventObserverMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
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
    public async Task GivenMultipleTransientMiddlewares_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
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
    public async Task GivenMultipleScopedMiddlewares_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>(ServiceLifetime.Scoped)
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
    public async Task GivenMultipleSingletonMiddlewares_ResolvingOrExecutingObserverOrDispatcherUsesSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>(ServiceLifetime.Singleton)
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
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingOrExecutingObserverOrDispatcherReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithMultipleMiddlewares>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>(ServiceLifetime.Singleton)
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
    public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());

        // every execution should execute 5 different middlewares (1x retry, 2x the others)
        Assert.That(observations.InvocationCounts, Is.EqualTo(Enumerable.Repeat(1, 30)));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1, 1, 1, 1, 2, 1, 1, 5, 1, 6, 1, 1, 7, 1, 8, 1, 1, 3, 1, 4, 1 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1, 1, 5, 1, 6, 1, 1, 7, 1, 8, 1, 1, 9, 1, 10, 1, 1, 11, 1, 12, 1 }));
    }

    [Test]
    public async Task GivenTransientObserverWithRetryMiddleware_EachRetryGetsNewObserverInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());

        // every execution should execute the observer 2 times
        Assert.That(observations.ObserverInvocationCounts, Is.EqualTo(Enumerable.Repeat(1, 12)));
    }

    [Test]
    public async Task GivenScopedObserverWithRetryMiddleware_EachRetryGetsObserverInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());

        Assert.That(observations.ObserverInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 1, 2, 5, 6, 7, 8, 3, 4 }));
    }

    [Test]
    public async Task GivenSingletonObserverWithRetryMiddleware_EachRetryGetsSameInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithRetryMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorEventObserverMiddleware<TestEventObserverRetryMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher1.DispatchEvent(new TestEvent());
        await dispatcher2.DispatchEvent(new TestEvent());

        Assert.That(observations.ObserverInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSameMiddlewareMultipleTimes>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
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

        Assert.That(observations.InvocationCounts, Is.EqualTo(Enumerable.Repeat(1, 16)));
    }

    [Test]
    public async Task GivenScopedMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSameMiddlewareMultipleTimes>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Scoped)
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

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 1, 2, 7, 8, 9, 10, 11, 12, 3, 4 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareThatIsAppliedMultipleTimes_EachExecutionUsesSameInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithSameMiddlewareMultipleTimes>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Singleton)
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

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }));
    }

    [Test]
    public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
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

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenTransientObserver_ServiceProviderInPipelineBuilderIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithDependencyDuringPipelineBuild>()
                    .AddScoped<DependencyResolvedDuringPipelineBuild>()
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

        Assert.That(observations.DependencyResolvedDuringPipelineBuildInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
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

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenScopedObserver_ServiceProviderInPipelineBuilderIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithDependencyDuringPipelineBuild>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringPipelineBuild>()
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

        Assert.That(observations.DependencyResolvedDuringPipelineBuildInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventObserverMiddleware<TestEventObserverMiddleware>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
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

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenSingletonObserver_ServiceProviderInPipelineBuilderIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserverWithDependencyDuringPipelineBuild>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringPipelineBuild>()
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

        Assert.That(observations.DependencyResolvedDuringPipelineBuildInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    private sealed record TestEvent;

    private sealed class TestEventObserver : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline) => pipeline.Use<TestEventObserverMiddleware>();
    }

    private sealed class TestEventObserverWithMultipleMiddlewares : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware>()
                        .Use<TestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverWithSameMiddlewareMultipleTimes : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware>()
                        .Use<TestEventObserverMiddleware>();
        }
    }

    private sealed class TestEventObserverWithRetryMiddleware(TestObservations observations) : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        private int invocationCount;

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.ObserverInvocationCounts.Add(invocationCount);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverRetryMiddleware>()
                        .Use<TestEventObserverMiddleware>()
                        .Use<TestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverWithDependencyDuringPipelineBuild : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline) => pipeline.ServiceProvider
                                                                                                .GetRequiredService<DependencyResolvedDuringPipelineBuild>()
                                                                                                .Execute();
    }

    private sealed class TestEventObserverMiddleware(TestObservations observations) : IEventObserverMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventObserverMiddleware2(TestObservations observations) : IEventObserverMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventObserverRetryMiddleware(TestObservations observations) : IEventObserverMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Event, ctx.CancellationToken);
            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class DependencyResolvedDuringMiddlewareExecution(TestObservations observations)
    {
        private int invocationCount;

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class DependencyResolvedDuringPipelineBuild(TestObservations observations)
    {
        private int invocationCount;

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringPipelineBuildInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> ObserverInvocationCounts { get; } = [];

        public List<int> InvocationCounts { get; } = [];

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = [];

        public List<int> DependencyResolvedDuringPipelineBuildInvocationCounts { get; } = [];
    }
}
