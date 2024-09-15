namespace Conqueror.Streaming.Tests;

public sealed class StreamConsumerMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_WhenCallingConsumerWithItems_CreatesNewInstanceForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareWithFactory_WhenCallingConsumerWithItems_CreatesNewInstanceForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware(p => new TestStreamConsumerMiddleware(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_WhenCallingConsumerWithItems_CreatesOneInstanceForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 4, 5, 6, 3, 4 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithFactory_WhenCallingConsumerWithItems_CreatesOneInstanceForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware(p => new TestStreamConsumerMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 4, 5, 6, 3, 4 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_WhenCallingConsumerWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithFactory_WhenCallingConsumerWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware(p => new TestStreamConsumerMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
    }

    [Test]
    public async Task GivenMultipleTransientMiddlewares_WhenCallingConsumerWithItems_CreatesNewInstancesForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares>()
                    .AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares3>(nameof(TestStreamConsumerWithMultipleMiddlewares3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares4>(nameof(TestStreamConsumerWithMultipleMiddlewares4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedMiddlewares_WhenCallingConsumerWithItems_CreatesOneInstanceEachForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares>()
                    .AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares3>(nameof(TestStreamConsumerWithMultipleMiddlewares3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares4>(nameof(TestStreamConsumerWithMultipleMiddlewares4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 2, 2, 4, 4, 5, 5, 6, 6, 3, 3, 4, 4 }));
    }

    [Test]
    public async Task GivenMultipleSingletonMiddlewares_WhenCallingConsumerWithItems_UsesSameInstancesAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares>()
                    .AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares3>(nameof(TestStreamConsumerWithMultipleMiddlewares3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares4>(nameof(TestStreamConsumerWithMultipleMiddlewares4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_WhenCallingConsumerWithItems_ReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares>()
                    .AddConquerorStreamConsumer<TestStreamConsumerWithMultipleMiddlewares2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares3>(nameof(TestStreamConsumerWithMultipleMiddlewares3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleMiddlewares4>(nameof(TestStreamConsumerWithMultipleMiddlewares4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumerWithMultipleMiddlewares4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7, 1, 8, 1, 9, 1, 10 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionCreatesNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesSingleInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
    }

    [Test]
    public async Task GivenTransientConsumerWithRetryMiddleware_EachRetryCreatesNewConsumerInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());

        Assert.That(observations.ConsumerInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedConsumerWithRetryMiddleware_EachRetryUsesConsumerInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());

        Assert.That(observations.ConsumerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonConsumerWithRetryMiddleware_EachRetryUsesSingleInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithRetryMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerRetryMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());

        Assert.That(observations.ConsumerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionCreatesNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithSameMiddlewareMultipleTimes>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await producer.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromConsumerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 4, 5, 6, 3, 4 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromConsumerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 4, 5, 6, 3, 4 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromConsumerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumer<TestStreamConsumer2>()
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer3>(nameof(TestStreamConsumer3))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer4>(nameof(TestStreamConsumer4))
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem2>>();
        var producer6 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer7 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer8 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));
        var producer9 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer3));
        var producer10 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem3>>(nameof(TestStreamConsumer4));

        await producer1.HandleItem(new());
        await producer2.HandleItem(new());
        await producer3.HandleItem(new());
        await producer4.HandleItem(new());
        await producer5.HandleItem(new());
        await producer6.HandleItem(new());
        await producer7.HandleItem(new());
        await producer8.HandleItem(new());
        await producer9.HandleItem(new());
        await producer10.HandleItem(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 4, 5, 6, 3, 4 }));
    }

    private sealed record TestItem;

    private sealed record TestItem2;

    private sealed record TestItem3;

    private sealed class TestStreamConsumer : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline) => pipeline.Use<TestStreamConsumerMiddleware>();
    }

    private sealed class TestStreamConsumer2 : IStreamConsumer<TestItem2>
    {
        public async Task HandleItem(TestItem2 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline) => pipeline.Use<TestStreamConsumerMiddleware>();
    }

    private sealed class TestStreamConsumer3 : IStreamConsumer<TestItem3>
    {
        public async Task HandleItem(TestItem3 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline) => pipeline.Use<TestStreamConsumerMiddleware>();
    }

    private sealed class TestStreamConsumer4 : IStreamConsumer<TestItem3>
    {
        public async Task HandleItem(TestItem3 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline) => pipeline.Use<TestStreamConsumerMiddleware>();
    }

    private sealed class TestStreamConsumerWithMultipleMiddlewares : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware>()
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithMultipleMiddlewares2 : IStreamConsumer<TestItem2>
    {
        public async Task HandleItem(TestItem2 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware>()
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithMultipleMiddlewares3 : IStreamConsumer<TestItem3>
    {
        public async Task HandleItem(TestItem3 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware>()
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithMultipleMiddlewares4 : IStreamConsumer<TestItem3>
    {
        public async Task HandleItem(TestItem3 item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware>()
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerWithSameMiddlewareMultipleTimes : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline) => pipeline.Use<TestStreamConsumerMiddleware>().Use<TestStreamConsumerMiddleware>();
    }

    private sealed class TestStreamConsumerWithRetryMiddleware(TestObservations observations) : IStreamConsumer<TestItem>
    {
        private int invocationCount;

        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.ConsumerInvocationCounts.Add(invocationCount);
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamConsumerRetryMiddleware>()
                        .Use<TestStreamConsumerMiddleware>()
                        .Use<TestStreamConsumerMiddleware2>();
        }
    }

    private sealed class TestStreamConsumerMiddleware(TestObservations observations) : IStreamConsumerMiddleware
    {
        private int invocationCount;

        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }

    private sealed class TestStreamConsumerMiddleware2(TestObservations observations) : IStreamConsumerMiddleware
    {
        private int invocationCount;

        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }

    private sealed class TestStreamConsumerRetryMiddleware(TestObservations observations) : IStreamConsumerMiddleware
    {
        private int invocationCount;

        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem> ctx)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await ctx.Next(ctx.Item, ctx.CancellationToken);
            await ctx.Next(ctx.Item, ctx.CancellationToken);
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
        public List<int> ConsumerInvocationCounts { get; } = [];

        public List<int> InvocationCounts { get; } = [];

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = [];
    }
}
