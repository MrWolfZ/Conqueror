namespace Conqueror.Streaming.Tests;

public sealed class StreamConsumerMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamConsumerMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline => { _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(initialConfiguration); });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new());

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamConsumerMiddlewareConfiguration(10);
        var overwrittenConfiguration = new TestStreamConsumerMiddlewareConfiguration(20);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(overwrittenConfiguration);
        });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new());

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamConsumerMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new());

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

        Assert.That(initialConfiguration.Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamConsumerMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(c => new(c.Parameter + 10));
        });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new());

        Assert.That(observations.Configurations, Has.Count.EqualTo(1));

        Assert.That(observations.Configurations[0].Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamConsumer<TestStreamConsumer>()
                    .AddConquerorStreamConsumerMiddleware<TestStreamConsumerMiddleware>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamConsumerPipelineBuilder>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(new TestStreamConsumerMiddlewareConfiguration(20)));
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(c => c.Parameter += 10));
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestStreamConsumerMiddleware, TestStreamConsumerMiddlewareConfiguration>(c => new(c.Parameter + 10)));
        });

        var provider = services.BuildServiceProvider();

        var consumer = provider.GetRequiredService<IStreamConsumer<TestItem>>();

        await consumer.HandleItem(new());
    }

    private sealed record TestItem;

    private sealed class TestStreamConsumer : IStreamConsumer<TestItem>
    {
        public async Task HandleItem(TestItem item, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IStreamConsumerPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IStreamConsumerPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestStreamConsumerMiddlewareConfiguration(int parameter)
    {
        public int Parameter { get; set; } = parameter;
    }

    private sealed class TestStreamConsumerMiddleware(TestObservations observations) : IStreamConsumerMiddleware<TestStreamConsumerMiddlewareConfiguration>
    {
        public async Task Execute<TItem>(StreamConsumerMiddlewareContext<TItem, TestStreamConsumerMiddlewareConfiguration> ctx)
        {
            await Task.Yield();
            observations.Configurations.Add(ctx.Configuration);
            await ctx.Next(ctx.Item, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<TestStreamConsumerMiddlewareConfiguration> Configurations { get; } = [];
    }
}
