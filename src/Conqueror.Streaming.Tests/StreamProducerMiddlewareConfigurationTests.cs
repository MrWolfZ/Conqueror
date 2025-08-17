namespace Conqueror.Streaming.Tests;

using System.Runtime.CompilerServices;

public sealed class StreamProducerMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddConquerorStreamProducer<TestStreamProducer>()
            .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
            .AddSingleton(observations);

        var initialConfiguration = new TestStreamProducerMiddlewareConfiguration(parameter: 10);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(
                initialConfiguration
            );
        });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(), CancellationToken.None).Drain(CancellationToken.None);

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddConquerorStreamProducer<TestStreamProducer>()
            .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
            .AddSingleton(observations);

        var initialConfiguration = new TestStreamProducerMiddlewareConfiguration(parameter: 10);
        var overwrittenConfiguration = new TestStreamProducerMiddlewareConfiguration(parameter: 20);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(
                initialConfiguration
            );

            _ = pipeline.Configure<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(
                overwrittenConfiguration
            );
        });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(), CancellationToken.None).Drain(CancellationToken.None);

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddConquerorStreamProducer<TestStreamProducer>()
            .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
            .AddSingleton(observations);

        var initialConfiguration = new TestStreamProducerMiddlewareConfiguration(parameter: 10);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(
                initialConfiguration
            );

            _ = pipeline.Configure<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(c =>
                c.Parameter += 10
            );
        });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(), CancellationToken.None).Drain(CancellationToken.None);

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

        Assert.That(initialConfiguration.Parameter, Is.EqualTo(expected: 20));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddConquerorStreamProducer<TestStreamProducer>()
            .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
            .AddSingleton(observations);

        var initialConfiguration = new TestStreamProducerMiddlewareConfiguration(parameter: 10);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(
                initialConfiguration
            );

            _ = pipeline.Configure<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(c =>
                new(c.Parameter + 10)
            );
        });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(), CancellationToken.None).Drain(CancellationToken.None);

        Assert.That(observations.Configurations, Has.Count.EqualTo(expected: 1));

        Assert.That(observations.Configurations[0].Parameter, Is.EqualTo(expected: 20));
    }

    [Test]
    public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services
            .AddConquerorStreamProducer<TestStreamProducer>()
            .AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>()
            .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamProducerPipelineBuilder>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() =>
                pipeline.Configure<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(
                    new TestStreamProducerMiddlewareConfiguration(parameter: 20)
                )
            );
            _ = Assert.Throws<InvalidOperationException>(() =>
                pipeline.Configure<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(c =>
                    c.Parameter += 10
                )
            );
            _ = Assert.Throws<InvalidOperationException>(() =>
                pipeline.Configure<TestStreamProducerMiddleware, TestStreamProducerMiddlewareConfiguration>(c =>
                    new(c.Parameter + 10)
                )
            );
        });

        var provider = services.BuildServiceProvider();

        var producer = provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>();

        _ = await producer.ExecuteRequest(new(), CancellationToken.None).Drain(CancellationToken.None);
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed class TestStreamProducer
        : IStreamProducer<TestStreamingRequest, TestItem>,
            IConfigureStreamProducerPipeline
    {
        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) =>
            pipeline.ServiceProvider.GetService<Action<IStreamProducerPipelineBuilder>>()?.Invoke(pipeline);

        public async IAsyncEnumerable<TestItem> ExecuteRequest(
            TestStreamingRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            yield return new();
        }
    }

    private sealed class TestStreamProducerMiddlewareConfiguration(int parameter)
    {
        public int Parameter { get; set; } = parameter;
    }

    private sealed class TestStreamProducerMiddleware(TestObservations observations)
        : IStreamProducerMiddleware<TestStreamProducerMiddlewareConfiguration>
    {
        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(
            StreamProducerMiddlewareContext<TRequest, TItem, TestStreamProducerMiddlewareConfiguration> ctx
        )
            where TRequest : class
        {
            await Task.Yield();
            observations.Configurations.Add(ctx.Configuration);

            await foreach (var item in ctx.Next(ctx.Request, ctx.CancellationToken))
            {
                yield return item;
            }
        }
    }

    private sealed class TestObservations
    {
        public List<TestStreamProducerMiddlewareConfiguration> Configurations { get; } = [];
    }
}
