using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public abstract class StreamingRequestClientMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingClientCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ResolvingClientCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ResolvingClientReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public async Task GivenMultipleTransientMiddlewares_ResolvingClientCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services,
                                                                                              CreateTransport,
                                                                                              p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenMultipleScopedMiddlewares_ResolvingClientCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services,
                                                                                              CreateTransport,
                                                                                              p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 2, 2 }));
    }

    [Test]
    public async Task GivenMultipleSingletonMiddlewares_ResolvingClientReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services,
                                                                                              CreateTransport,
                                                                                              p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingClientReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services,
                                                                                              CreateTransport,
                                                                                              p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestRetryMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestRetryMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestRetryMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
    }

    [Test]
    public async Task GivenTransientTransportWithRetryMiddleware_EachRetryGetsNewTransportInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            b => b.ServiceProvider.GetRequiredService<TestStreamingRequestTransport>(),
                                                                                            p => p.Use<TestStreamingRequestRetryMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware>()
                                                                                                  .Use<TestStreamingRequestMiddleware2>());

        _ = services.AddTransient<TestStreamingRequestTransport>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.TransportInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services,
                                                                                            CreateTransport,
                                                                                            p => p.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task GivenTransientMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());
        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenScopedMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());
        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonMiddleware_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest, TestItem>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());
        AddStreamingRequestClient<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>(services, CreateTransport, p => p.Use<TestStreamingRequestMiddleware>());

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest2, TestItem2>>();

        _ = await handler1.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler2.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler3.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler4.ExecuteRequest(new(), CancellationToken.None).Drain();
        _ = await handler5.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    protected abstract void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler;

    private static IStreamingRequestTransportClient CreateTransport(IStreamingRequestTransportClientBuilder builder)
    {
        return new TestStreamingRequestTransport(builder.ServiceProvider.GetRequiredService<TestObservations>());
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestStreamingRequest2;

    private sealed record TestItem2;

    private sealed class TestStreamingRequestMiddleware : IStreamingRequestMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class TestStreamingRequestMiddleware2 : IStreamingRequestMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class TestStreamingRequestRetryMiddleware : IStreamingRequestMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
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

    private sealed class TestStreamingRequestTransport : IStreamingRequestTransportClient
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestTransport(TestObservations observations)
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
public sealed class StreamingRequestClientMiddlewareLifetimeWithSyncFactoryTests : StreamingRequestClientMiddlewareLifetimeTests
{
    protected override void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamingRequestClient<THandler>(transportClientFactory, configurePipeline);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class StreamingRequestClientMiddlewareLifetimeWithAsyncFactoryTests : StreamingRequestClientMiddlewareLifetimeTests
{
    protected override void AddStreamingRequestClient<THandler>(IServiceCollection services,
                                                                Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                                Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        _ = services.AddConquerorStreamingRequestClient<THandler>(async b =>
                                                                  {
                                                                      await Task.Delay(1);
                                                                      return transportClientFactory(b);
                                                                  },
                                                                  configurePipeline);
    }
}
