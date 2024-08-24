namespace Conqueror.Streaming.Tests;

public sealed class StreamConsumerLifetimeTests
{
    [Test]
    public async Task GivenTransientConsumer_WhenCallingItWithItems_CreatesNewInstanceForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task GivenKeyedTransientConsumer_WhenCallingItWithItems_CreatesNewInstanceForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer))
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer2>(nameof(TestStreamConsumer2))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientConsumerWithFactory_WhenCallingItWithItems_CreatesNewInstanceForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer(p => new TestStreamConsumer(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public async Task GivenKeyedTransientConsumerWithFactory_WhenCallingItWithItems_CreatesNewInstanceForEveryItem()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer), (p, _) => new TestStreamConsumer(p.GetRequiredService<TestObservations>()))
                    .AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer2), (p, _) => new TestStreamConsumer2(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedConsumer_WhenCallingItWithItems_CreatesOneInstanceForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
    }

    [Test]
    public async Task GivenKeyedScopedConsumer_WhenCallingItWithItems_CreatesOneInstanceForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer), ServiceLifetime.Scoped)
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer2>(nameof(TestStreamConsumer2), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenScopedConsumerWithFactory_WhenCallingItWithItems_CreatesOneInstanceForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer(p => new TestStreamConsumer(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
    }

    [Test]
    public async Task GivenKeyedScopedConsumerWithFactory_WhenCallingItWithItems_CreatesOneInstanceForAllItemsInScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer), (p, _) => new TestStreamConsumer(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer2), (p, _) => new TestStreamConsumer2(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonConsumer_WhenCallingItWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenKeyedSingletonConsumer_WhenCallingItWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer), ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer2>(nameof(TestStreamConsumer2), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonConsumerWithFactory_WhenCallingItWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer(p => new TestStreamConsumer(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenKeyedSingletonConsumerWithFactory_WhenCallingItWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer), (p, _) => new TestStreamConsumer(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer2), (p, _) => new TestStreamConsumer2(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 3 }));
    }

    [Test]
    public async Task GivenSingletonConsumerWithMultipleConsumerInterfaces_WhenCallingItWithItems_ForEitherItemTypeUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumerWithMultipleInterfaces>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = provider.GetRequiredService<IStreamConsumer<TestItem2>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenKeyedSingletonConsumerWithMultipleConsumerInterfaces_WhenCallingItWithItems_ForEitherItemTypeUsesSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleInterfaces>(1, ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumerWithMultipleInterfaces>(2, ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(1);
        var consumer2 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem2>>(1);
        var consumer3 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(2);
        var consumer4 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem2>>(2);

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonConsumer_WhenResolvingConsumerDirectly_UsesSameInstanceAsCreatingConsumerViaFactory()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredService<TestStreamConsumer>();
        var consumer2 = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public async Task GivenKeyedSingletonConsumer_WhenResolvingConsumerDirectly_UsesSameInstanceAsCreatingConsumerViaFactory()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumerKeyed<TestStreamConsumer>(nameof(TestStreamConsumer), ServiceLifetime.Singleton)
                    .AddConquerorStreamConsumerKeyed<TestStreamConsumer2>(nameof(TestStreamConsumer2), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var consumer1 = provider.GetRequiredKeyedService<TestStreamConsumer>(nameof(TestStreamConsumer));
        var consumer2 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = provider.GetRequiredKeyedService<TestStreamConsumer2>(nameof(TestStreamConsumer2));
        var consumer4 = provider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonConsumerInstance_WhenCallingItWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var instance = new TestStreamConsumer(observations);

        _ = services.AddConquerorStreamConsumer(instance);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer2 = scope1.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();
        var consumer3 = scope2.ServiceProvider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public async Task GivenKeyedSingletonConsumerInstance_WhenCallingItWithItems_UsesSameInstanceAcrossMultipleScopes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var instance1 = new TestStreamConsumer(observations);
        var instance2 = new TestStreamConsumer2(observations);

        _ = services.AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer), instance1)
                    .AddConquerorStreamConsumerKeyed(nameof(TestStreamConsumer2), instance2);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var consumer1 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer2 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer3 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer));
        var consumer4 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer5 = scope1.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));
        var consumer6 = scope2.ServiceProvider.GetRequiredKeyedService<IStreamConsumer<TestItem>>(nameof(TestStreamConsumer2));

        await consumer1.HandleItem(new());
        await consumer2.HandleItem(new());
        await consumer3.HandleItem(new());
        await consumer4.HandleItem(new());
        await consumer5.HandleItem(new());
        await consumer6.HandleItem(new());

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2, 3 }));
    }

    private sealed record TestItem;

    private sealed record TestItem2;

    private sealed class TestStreamConsumer(TestObservations observations) : IStreamConsumer<TestItem>
    {
        private int invocationCount;

        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestStreamConsumer2(TestObservations observations) : IStreamConsumer<TestItem>
    {
        private int invocationCount;

        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestStreamConsumerWithMultipleInterfaces(TestObservations observations) : IStreamConsumer<TestItem>,
                                                                                                   IStreamConsumer<TestItem2>
    {
        private int invocationCount;

        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
        }

        public async Task HandleItem(TestItem2 item, CancellationToken cancellationToken = default)
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
