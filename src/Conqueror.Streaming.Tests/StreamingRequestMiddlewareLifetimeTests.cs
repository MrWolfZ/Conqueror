using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamingRequestMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingHandlerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
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
    public async Task GivenTransientMiddlewareWithFactory_ResolvingHandlerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware(p => new TestStreamingRequestMiddleware(p.GetRequiredService<TestObservations>()))
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
    public async Task GivenScopedMiddleware_ResolvingHandlerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
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
    public async Task GivenScopedMiddlewareWithFactory_ResolvingHandlerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware(p => new TestStreamingRequestMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
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
    public async Task GivenSingletonMiddleware_ResolvingHandlerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
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
    public async Task GivenSingletonMiddlewareWithFactory_ResolvingHandlerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware(p => new TestStreamingRequestMiddleware(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
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
    public async Task GivenMultipleTransientMiddlewares_ResolvingHandlerCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
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
    public async Task GivenMultipleScopedMiddlewares_ResolvingHandlerCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
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
    public async Task GivenMultipleSingletonMiddlewares_ResolvingHandlerReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
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
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingHandlerReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithMultipleMiddlewares2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithRetryMiddleware>()
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

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithRetryMiddleware>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestRetryMiddleware>()
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
    public async Task GivenTransientHandlerWithRetryMiddleware_EachRetryGetsNewHandlerInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithRetryMiddleware>()
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

        Assert.That(observations.HandlerInvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedHandlerWithRetryMiddleware_EachRetryGetsHandlerInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithRetryMiddleware>(ServiceLifetime.Scoped)
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

        Assert.That(observations.HandlerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1, 2 }));
    }

    [Test]
    public async Task GivenSingletonHandlerWithRetryMiddleware_EachRetryGetsSameInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithRetryMiddleware>(ServiceLifetime.Singleton)
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

        Assert.That(observations.HandlerInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task GivenTransientMiddlewareThatIsAppliedMultipleTimes_EachExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandlerWithSameMiddlewareMultipleTimes>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Scoped)
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

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestHandler<TestStreamingRequestHandler2>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(ServiceLifetime.Singleton)
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

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed record TestStreamingRequest2;

    private sealed record TestItem2;

    private sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline) => pipeline.Use<TestStreamingRequestMiddleware>();
    }

    private sealed class TestStreamingRequestHandler2 : IStreamingRequestHandler<TestStreamingRequest2, TestItem2>, IConfigureStreamingRequestPipeline
    {
        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline) => pipeline.Use<TestStreamingRequestMiddleware>();
    }

    private sealed class TestStreamingRequestHandlerWithMultipleMiddlewares : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamingRequestMiddleware>()
                        .Use<TestStreamingRequestMiddleware2>();
        }
    }

    private sealed class TestStreamingRequestHandlerWithMultipleMiddlewares2 : IStreamingRequestHandler<TestStreamingRequest2, TestItem2>, IConfigureStreamingRequestPipeline
    {
        public async IAsyncEnumerable<TestItem2> ExecuteRequest(TestStreamingRequest2 request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamingRequestMiddleware>()
                        .Use<TestStreamingRequestMiddleware2>();
        }
    }

    private sealed class TestStreamingRequestHandlerWithSameMiddlewareMultipleTimes : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline) => pipeline.Use<TestStreamingRequestMiddleware>().Use<TestStreamingRequestMiddleware>();
    }

    private sealed class TestStreamingRequestHandlerWithRetryMiddleware : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestStreamingRequestHandlerWithRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.HandlerInvocationCounts.Add(invocationCount);
            yield return new();
            yield return new();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestStreamingRequestRetryMiddleware>()
                        .Use<TestStreamingRequestMiddleware>()
                        .Use<TestStreamingRequestMiddleware2>();
        }
    }

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
        public List<int> HandlerInvocationCounts { get; } = new();

        public List<int> InvocationCounts { get; } = new();

        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = new();
    }
}
