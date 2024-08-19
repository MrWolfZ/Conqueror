using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public abstract class StreamProducerClientMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingClientCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
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
    public async Task GivenScopedMiddleware_ResolvingClientCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
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
    public async Task GivenSingletonMiddleware_ResolvingClientReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
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
    public async Task GivenMultipleTransientMiddlewares_ResolvingClientCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services,
                                                                                     CreateTransport,
                                                                                     p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
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
    public async Task GivenMultipleScopedMiddlewares_ResolvingClientCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services,
                                                                                     CreateTransport,
                                                                                     p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
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
    public async Task GivenMultipleSingletonMiddlewares_ResolvingClientReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services,
                                                                                     CreateTransport,
                                                                                     p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
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
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingClientReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services,
                                                                                     CreateTransport,
                                                                                     p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
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

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerRetryMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
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

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerRetryMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
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

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerRetryMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware2>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerRetryMiddleware>()
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
    public async Task GivenTransientTransportWithRetryMiddleware_EachRetryGetsNewTransportInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   b => b.ServiceProvider.GetRequiredService<TestStreamProducerTransport>(),
                                                                                   p => p.Use<TestStreamProducerRetryMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware>()
                                                                                         .Use<TestStreamProducerMiddleware2>());

        _ = services.AddTransient<TestStreamProducerTransport>()
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

        Assert.That(observations.TransportInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services,
                                                                                   CreateTransport,
                                                                                   p => p.Use<TestStreamProducerMiddleware>().Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
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

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());
        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
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

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());
        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Scoped)
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

        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());
        AddStreamingRequestClient<IStreamProducer<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamProducerMiddleware>());

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(ServiceLifetime.Singleton)
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

    protected abstract void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer;

    private static IStreamProducerTransportClient CreateTransport(IStreamProducerTransportClientBuilder builder)
    {
        return new TestStreamProducerTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestStreamingRequest2;

    private sealed record TestItem2;

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

    private sealed class TestStreamProducerTransport : IStreamProducerTransportClient
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamProducerTransport(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> ExecuteRequest<TRequest, TItem>(TRequest request,
                                                                             IServiceProvider serviceProvider,
                                                                             [EnumeratorCancellation] CancellationToken cancellationToken)
            where TRequest : class
        {
            await Task.Yield();

            invocationCount += 1;
            observations.TransportInvocationCounts.Add(invocationCount);

            if (typeof(TRequest) == typeof(TestStreamingRequest))
            {
                yield return (TItem)(object)new TestItem();
                yield break;
            }

            if (typeof(TRequest) == typeof(TestStreamingRequest2))
            {
                yield return (TItem)(object)new TestItem2();
                yield break;
            }

            throw new InvalidOperationException("should never reach this");
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
        public List<int> TransportInvocationCounts { get; } = new();

        public List<int> InvocationCounts { get; } = new();

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = new();
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamProducerClientMiddlewareLifetimeWithSyncFactoryTests : StreamProducerClientMiddlewareLifetimeTests
{
    protected override void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamProducerClient<TProducer>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamProducerClientMiddlewareLifetimeWithAsyncFactoryTests : StreamProducerClientMiddlewareLifetimeTests
{
    protected override void AddStreamingRequestClient<TProducer>(IServiceCollection services,
                                                                 Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                                 Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamProducerClient<TProducer>(async b =>
                                                                 {
                                                                     await Task.Delay(1);
                                                                     return transportClientFactory(b);
                                                                 },
                                                                 configurePipeline);
    }
}
