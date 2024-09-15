namespace Conqueror.Eventing.Tests;

public sealed class EventPublisherMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var initialConfiguration = new TestEventPublisherMiddlewareConfiguration(10);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(initialConfiguration))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var initialConfiguration = new TestEventPublisherMiddlewareConfiguration(10);
        var overwrittenConfiguration = new TestEventPublisherMiddlewareConfiguration(20);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(initialConfiguration)
                                                                            .Configure<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(overwrittenConfiguration))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var initialConfiguration = new TestEventPublisherMiddlewareConfiguration(10);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(initialConfiguration)
                                                                            .Configure<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(c => c.Parameter += 10))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

        Assert.That(initialConfiguration.Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        var initialConfiguration = new TestEventPublisherMiddlewareConfiguration(10);

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline => pipeline.Use<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(initialConfiguration)
                                                                            .Configure<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(c => new(c.Parameter + 10)))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());

        Assert.That(observations.Configurations, Has.Count.EqualTo(1));

        Assert.That(observations.Configurations[0].Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddConquerorEventPublisherMiddleware<TestEventPublisherMiddleware>()
                    .AddConquerorInMemoryEventPublisher(pipeline =>
                    {
                        _ = Assert.Throws<InvalidOperationException>(
                            () => pipeline.Configure<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(new TestEventPublisherMiddlewareConfiguration(20)));
                        _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(c => c.Parameter += 10));
                        _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestEventPublisherMiddleware, TestEventPublisherMiddlewareConfiguration>(c => new(c.Parameter + 10)));
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await observer.HandleEvent(new());
    }

    private sealed record TestEvent;

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public async Task HandleEvent(TestEvent query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }
    }

    private sealed class TestEventPublisherMiddlewareConfiguration(int parameter)
    {
        public int Parameter { get; set; } = parameter;
    }

    private sealed class TestEventPublisherMiddleware(TestObservations observations) : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>
    {
        public async Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class
        {
            await Task.Yield();
            observations.Configurations.Add(ctx.Configuration);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<TestEventPublisherMiddlewareConfiguration> Configurations { get; } = [];
    }
}
