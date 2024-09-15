namespace Conqueror.Eventing.Tests;

public sealed class EventPublisherMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
                    .AddConquerorEventPublisherMiddleware(p => new TestEventPublisherMiddleware(p.GetRequiredService<TestObservations>()))
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
                    .AddConquerorEventPublisherMiddleware(p => new TestEventPublisherMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
                    .AddConquerorEventPublisherMiddleware(p => new TestEventPublisherMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>(ServiceLifetime.Scoped)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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
    public async Task GivenMultipleSingletonMiddlewares_ResolvingOrExecutingObserverOrDispatcherReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>(ServiceLifetime.Singleton)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>(ServiceLifetime.Singleton)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Scoped)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware2>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware2>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>())
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

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>()
                                                                            .Use<TestEventPublisherMiddleware>())
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
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
    public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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
    public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware>())
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

    private sealed record TestEvent;

    private sealed class TestEventObserver(TestObservations observations) : IEventObserver<TestEvent>
    {
        private int invocationCount;

        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.ObserverInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestEventPublisherMiddleware(TestObservations observations) : IEventPublisherMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventPublisherMiddleware2(TestObservations observations) : IEventPublisherMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventPublisherRetryMiddleware(TestObservations observations) : IEventPublisherMiddleware
    {
        private int invocationCount;

        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
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

    private sealed class TestObservations
    {
        public List<int> ObserverInvocationCounts { get; } = [];

        public List<int> InvocationCounts { get; } = [];

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = [];
    }
}
