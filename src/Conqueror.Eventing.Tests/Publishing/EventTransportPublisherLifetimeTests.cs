using System.Collections.Concurrent;

namespace Conqueror.Eventing.Tests.Publishing;

public sealed class EventTransportPublisherLifetimeTests
{
    [Test]
    public async Task GivenTransientPublisher_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientPublisherWithFactory_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(p => new TestEventTransportPublisher1(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedPublisher_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenScopedPublisherWithFactory_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(p => new TestEventTransportPublisher1(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenSingletonPublisher_ResolvingOrExecutingObserverOrDispatcherUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public async Task GivenSingletonPublisherWithFactory_ResolvingOrExecutingObserverOrDispatcherUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher(p => new TestEventTransportPublisher1(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
    }

    [Test]
    public async Task GivenMultipleTransientPublishers_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher3.DispatchEvent(new TestEventWithMultiplePublishers());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedPublishers_ResolvingOrExecutingObserverOrDispatcherCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(ServiceLifetime.Scoped)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher3.DispatchEvent(new TestEventWithMultiplePublishers());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 4, 4, 5, 5, 6, 6, 2, 2 }));
    }

    [Test]
    public async Task GivenMultipleSingletonPublishers_ResolvingOrExecutingObserverOrDispatcherUsesSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher3.DispatchEvent(new TestEventWithMultiplePublishers());

        Assert.That(observations.InvocationCounts, Is.EqualTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8 }));
    }

    [Test]
    public async Task GivenMultiplePublishersWithDifferentLifetimes_ResolvingOrExecutingObserverOrDispatcherReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher3.DispatchEvent(new TestEventWithMultiplePublishers());

        // NOTE: do not use EqualTo since publisher execution order is not guaranteed
        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1, 8 }));
    }

    [Test]
    public async Task GivenTransientPublisherWithRetryMiddleware_EachPublisherExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>())
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());

        // every execution should execute 4 different middlewares (2x for each publisher)
        Assert.That(observations.InvocationCounts, Is.EqualTo(Enumerable.Repeat(1, 24)));
    }

    [Test]
    public async Task GivenScopedPublisherWithRetryMiddleware_EachPublisherExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>(), ServiceLifetime.Scoped)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());

        // NOTE: do not use EqualTo since publisher execution order is not guaranteed
        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 1, 3, 1, 4, 1, 1, 1, 2, 1, 5, 1, 6, 1, 7, 1, 8, 1, 3, 1, 4, 1 }));
    }

    [Test]
    public async Task GivenSingletonPublisherWithRetryMiddleware_EachPublisherExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherRetryMiddleware>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>(), ServiceLifetime.Singleton)
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher2>(pipeline => pipeline.Use<TestEventPublisherRetryMiddleware>())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithMultiplePublishers>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher1.DispatchEvent(new TestEventWithMultiplePublishers());
        await dispatcher2.DispatchEvent(new TestEventWithMultiplePublishers());

        // NOTE: do not use EqualTo since publisher execution order is not guaranteed
        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1, 8, 1, 9, 1, 10, 1, 11, 1, 12, 1 }));
    }

    [Test]
    public async Task GivenTransientPublisher_ServiceProviderParameterIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>()
                    .AddScoped<DependencyResolvedDuringPublisherExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.DependencyResolvedDuringPublisherExecutionInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenTransientPublisher_ServiceProviderInPipelineBuilderIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(pipeline => pipeline.ServiceProvider
                                                                                                           .GetRequiredService<DependencyResolvedDuringPublisherPipelineBuild>()
                                                                                                           .Execute())
                    .AddScoped<DependencyResolvedDuringPublisherPipelineBuild>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.DependencyResolvedDuringPublisherPipelineBuildInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenScopedPublisher_ServiceProviderParameterIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringPublisherExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.DependencyResolvedDuringPublisherExecutionInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenScopedPublisher_ServiceProviderInPipelineBuilderIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(pipeline => pipeline.ServiceProvider
                                                                                                           .GetRequiredService<DependencyResolvedDuringPublisherPipelineBuild>()
                                                                                                           .Execute(),
                                                                                       ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringPublisherPipelineBuild>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.DependencyResolvedDuringPublisherPipelineBuildInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenSingletonPublisher_ServiceProviderParameterIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringPublisherExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.DependencyResolvedDuringPublisherExecutionInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [Test]
    public async Task GivenSingletonPublisher_ServiceProviderInPipelineBuilderIsFromObserverOrDispatcherResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventTransportPublisher<TestEventTransportPublisher1>(pipeline => pipeline.ServiceProvider
                                                                                                           .GetRequiredService<DependencyResolvedDuringPublisherPipelineBuild>()
                                                                                                           .Execute(),
                                                                                       ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringPublisherPipelineBuild>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer2 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();
        var observer3 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEventWithCustomPublisher>>();

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher2 = scope1.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();
        var dispatcher3 = scope2.ServiceProvider.GetRequiredService<IConquerorEventDispatcher>();

        await observer1.HandleEvent(new());
        await observer1.HandleEvent(new());
        await observer2.HandleEvent(new());
        await observer3.HandleEvent(new());

        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher1.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher2.DispatchEvent(new TestEventWithCustomPublisher());
        await dispatcher3.DispatchEvent(new TestEventWithCustomPublisher());

        Assert.That(observations.DependencyResolvedDuringPublisherPipelineBuildInvocationCounts, Is.EqualTo(new[] { 1, 2, 3, 1, 4, 5, 6, 2 }));
    }

    [TestEventPublisher1]
    private sealed record TestEventWithCustomPublisher;

    [TestEventPublisher1]
    [TestEventPublisher2]
    private sealed record TestEventWithMultiplePublishers;

    private sealed class TestEventObserver : IEventObserver<TestEventWithCustomPublisher>,
                                             IEventObserver<TestEventWithMultiplePublishers>
    {
        public async Task HandleEvent(TestEventWithCustomPublisher evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public async Task HandleEvent(TestEventWithMultiplePublishers evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventPublisher1Attribute : Attribute, IConquerorEventTransportConfigurationAttribute;

    private sealed class TestEventTransportPublisher1(TestObservations observations) : IConquerorEventTransportPublisher<TestEventPublisher1Attribute>
    {
        private int invocationCount;

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventPublisher1Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Enqueue(invocationCount);

            serviceProvider.GetService<DependencyResolvedDuringPublisherExecution>()?.Execute();
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventPublisher2Attribute : Attribute, IConquerorEventTransportConfigurationAttribute;

    private sealed class TestEventTransportPublisher2(TestObservations observations) : IConquerorEventTransportPublisher<TestEventPublisher2Attribute>
    {
        private int invocationCount;

        public async Task PublishEvent<TEvent>(TEvent evt, TestEventPublisher2Attribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Enqueue(invocationCount);

            serviceProvider.GetService<DependencyResolvedDuringPublisherExecution>()?.Execute();
        }
    }

    private sealed class TestEventPublisherRetryMiddleware : IEventPublisherMiddleware
    {
        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            await Task.Yield();

            await ctx.Next(ctx.Event, ctx.CancellationToken);
            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class DependencyResolvedDuringPublisherExecution(TestObservations observations)
    {
        private int invocationCount;

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringPublisherExecutionInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class DependencyResolvedDuringPublisherPipelineBuild(TestObservations observations)
    {
        private int invocationCount;

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringPublisherPipelineBuildInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public ConcurrentQueue<int> InvocationCounts { get; } = new();

        public List<int> DependencyResolvedDuringPublisherExecutionInvocationCounts { get; } = [];

        public List<int> DependencyResolvedDuringPublisherPipelineBuildInvocationCounts { get; } = [];
    }
}
