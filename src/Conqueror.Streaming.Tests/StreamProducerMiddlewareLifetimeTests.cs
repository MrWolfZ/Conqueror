using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamProducerMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingProducerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareWithFactory_ResolvingProducerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware(p => new TestStreamProducerMiddleware(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ResolvingProducerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithFactory_ResolvingProducerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware(p => new TestStreamProducerMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ResolvingProducerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithFactory_ResolvingProducerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware(p => new TestStreamProducerMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task GivenMultipleTransientMiddlewares_ResolvingProducerCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares>()
                    .AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedMiddlewares_ResolvingProducerCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares>()
                    .AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 2, 2 }));
    }

    [Test]
    public async Task GivenMultipleSingletonMiddlewares_ResolvingProducerReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares>()
                    .AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingProducerReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares>()
                    .AddConquerorStreamProducer<TestStreamProducerWithMultipleMiddlewares2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
    }

    [Test]
    public async Task GivenTransientProducerWithRetryMiddleware_EachRetryGetsNewProducerInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.ProducerInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedProducerWithRetryMiddleware_EachRetryGetsProducerInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.ProducerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonProducerWithRetryMiddleware_EachRetryGetsSameInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithRetryMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.ProducerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducerWithSameMiddlewareMultipleTimes>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromProducerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromProducerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromProducerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamProducer<TestStreamProducer>()
                    .AddConquerorStreamProducer<TestStreamProducer2>()
                    .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var producer1 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer2 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer3 = scope1.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();
        var producer4 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();
        var producer5 = scope2.ServiceProvider.GetRequiredService<IStreamProducer<TestStreamingRequest2, TestItem2>>();

        _ = await producer1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await producer5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestStreamingRequest2;

    private sealed record TestItem2;

    private sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) => pipeline.Use<TestStreamProducerMiddleware>();
    }

    private sealed class TestStreamProducer2 : IStreamProducer<TestStreamingRequest2, TestItem2>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) => pipeline.Use<TestStreamProducerMiddleware>();
    }

    private sealed class TestStreamProducerWithMultipleMiddlewares : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerMiddleware>()
                        .Use<TestStreamProducerMiddleware2>();
        }
    }

    private sealed class TestStreamProducerWithMultipleMiddlewares2 : IStreamProducer<TestStreamingRequest2, TestItem2>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerMiddleware>()
                        .Use<TestStreamProducerMiddleware2>();
        }
    }

    private sealed class TestStreamProducerWithSameMiddlewareMultipleTimes : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) => pipeline.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware>();
    }

    private sealed class TestStreamProducerWithRetryMiddleware : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducerWithRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.ProducerInvocationCounts.Add(invocationCount);
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamProducerRetryMiddleware>()
                        .Use<TestStreamProducerMiddleware>()
                        .Use<TestStreamProducerMiddleware2>();
        }
    }

    private sealed class TestStreamProducerMiddleware : IStreamProducerMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducerMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestStreamProducerMiddleware2 : IStreamProducerMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducerMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestStreamProducerRetryMiddleware : IStreamProducerMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducerRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                // discard items from first attempt
                _ = item;
            }

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class DependencyResolvedDuringMiddlewareExecution
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public DependencyResolvedDuringMiddlewareExecution(TestObservations observations)
        {
            this.observations = observations;
        }

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> ProducerInvocationCounts { get; } = new();

        public List<int> InvocationCounts { get; } = new();

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = new();
    }
}
