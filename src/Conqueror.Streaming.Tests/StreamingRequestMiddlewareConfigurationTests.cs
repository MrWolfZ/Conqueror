using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamingRequestMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamingRequestMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<IStreamingRequestPipelineBuilder>>(pipeline => { _ = pipeline.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(initialConfiguration); });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamingRequestMiddlewareConfiguration(10);
        var overwrittenConfiguration = new TestStreamingRequestMiddlewareConfiguration(20);

        _ = services.AddSingleton<Action<IStreamingRequestPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(overwrittenConfiguration);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamingRequestMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<IStreamingRequestPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

        Assert.That(initialConfiguration.Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestStreamingRequestMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<IStreamingRequestPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(c => new(c.Parameter + 10));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(), CancellationToken.None).Drain();

        Assert.That(observations.Configurations, Has.Count.EqualTo(1));

        Assert.That(observations.Configurations[0].Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>()
                    .AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IStreamingRequestPipelineBuilder>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(new TestStreamingRequestMiddlewareConfiguration(20)));
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(c => c.Parameter += 10));
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestStreamingRequestMiddleware, TestStreamingRequestMiddlewareConfiguration>(c => new(c.Parameter + 10)));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>();

        _ = await handler.ExecuteRequest(new(), CancellationToken.None).Drain();
    }

    private sealed record TestStreamingRequest;

    private sealed record TestItem;

    private sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new();
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IStreamingRequestPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestStreamingRequestMiddlewareConfiguration
    {
        public TestStreamingRequestMiddlewareConfiguration(int parameter)
        {
            Parameter = parameter;
        }

        public int Parameter { get; set; }
    }

    private sealed class TestStreamingRequestMiddleware : IStreamingRequestMiddleware<TestStreamingRequestMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestStreamingRequestMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem, TestStreamingRequestMiddlewareConfiguration> ctx)
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
        public List<TestStreamingRequestMiddlewareConfiguration> Configurations { get; } = new();
    }
}
