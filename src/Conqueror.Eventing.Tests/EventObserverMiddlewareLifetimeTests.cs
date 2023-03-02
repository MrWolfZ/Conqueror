namespace Conqueror.Eventing.Tests;

public sealed class EventObserverMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenTransientMiddleware_ResolvingObserverCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddTransient<TestEventObserverMiddleware>()
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
    public async Task GivenScopedMiddleware_ResolvingObserverCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddScoped<TestEventObserverMiddleware>()
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
    public async Task GivenSingletonMiddleware_ResolvingObserverReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserver>()
                    .AddSingleton<TestEventObserverMiddleware>()
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
    public async Task GivenMultipleTransientMiddlewares_ResolvingObserverCreatesNewInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                    .AddTransient<TestEventObserverMiddleware>()
                    .AddTransient<TestEventObserverMiddleware2>()
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
    public async Task GivenMultipleScopedMiddlewares_ResolvingObserverCreatesNewInstancesForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                    .AddScoped<TestEventObserverMiddleware>()
                    .AddScoped<TestEventObserverMiddleware2>()
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
    public async Task GivenMultipleSingletonMiddlewares_ResolvingObserverReturnsSameInstancesEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                    .AddSingleton<TestEventObserverMiddleware>()
                    .AddSingleton<TestEventObserverMiddleware2>()
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
    public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingObserverReturnsInstancesAccordingToEachLifetime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithMultipleMiddlewares>()
                    .AddTransient<TestEventObserverMiddleware>()
                    .AddSingleton<TestEventObserverMiddleware2>()
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
    public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithRetryMiddleware>()
                    .AddTransient<TestEventObserverRetryMiddleware>()
                    .AddTransient<TestEventObserverMiddleware>()
                    .AddTransient<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
    }

    [Test]
    public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithRetryMiddleware>()
                    .AddTransient<TestEventObserverRetryMiddleware>()
                    .AddScoped<TestEventObserverMiddleware>()
                    .AddTransient<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
    }

    [Test]
    public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing()
                    .AddTransient<TestEventObserverWithRetryMiddleware>()
                    .AddTransient<TestEventObserverRetryMiddleware>()
                    .AddSingleton<TestEventObserverMiddleware>()
                    .AddTransient<TestEventObserverMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = scope1.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = scope2.ServiceProvider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer1.HandleEvent(new(), CancellationToken.None);
        await observer2.HandleEvent(new(), CancellationToken.None);

        Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
    }

    private sealed record TestEvent;

    private sealed class TestEventObserver : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline) => pipeline.Use<TestEventObserverMiddleware>();
    }

    private sealed class TestEventObserverWithMultipleMiddlewares : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverMiddleware>()
                        .Use<TestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverWithRetryMiddleware : IEventObserver<TestEvent>, IConfigureEventObserverPipeline
    {
        public async Task HandleEvent(TestEvent evt, CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestEventObserverRetryMiddleware>()
                        .Use<TestEventObserverMiddleware>()
                        .Use<TestEventObserverMiddleware2>();
        }
    }

    private sealed class TestEventObserverMiddleware : IEventObserverMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventObserverMiddleware2 : IEventObserverMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventObserverRetryMiddleware : IEventObserverMiddleware
    {
        private readonly TestObservations observations;
        private int invocationCount;

        public TestEventObserverRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = new();
    }
}
