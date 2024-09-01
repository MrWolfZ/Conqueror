namespace Conqueror.CQS.Tests.CommandHandling;

public sealed class CommandMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithConfiguration_InitialConfigurationIsPassedToMiddleware()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestCommandMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline => { _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration); });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeOverwrittenFully()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestCommandMiddlewareConfiguration(10);
        var overwrittenConfiguration = new TestCommandMiddlewareConfiguration(20);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(overwrittenConfiguration);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { overwrittenConfiguration }));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedInPlace()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestCommandMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.Configurations, Is.EquivalentTo(new[] { initialConfiguration }));

        Assert.That(initialConfiguration.Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenMiddlewareWithConfiguration_ConfigurationCanBeUpdatedAndReplaced()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var initialConfiguration = new TestCommandMiddlewareConfiguration(10);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
        {
            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(initialConfiguration);

            _ = pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => new(c.Parameter + 10));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);

        Assert.That(observations.Configurations, Has.Count.EqualTo(1));

        Assert.That(observations.Configurations[0].Parameter, Is.EqualTo(20));
    }

    [Test]
    public async Task GivenUnusedMiddlewareWithConfiguration_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipelineBuilder>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new TestCommandMiddlewareConfiguration(20)));
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => c.Parameter += 10));
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(c => new(c.Parameter + 10)));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.ExecuteCommand(new(), CancellationToken.None);
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<ICommandPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestCommandMiddlewareConfiguration
    {
        public TestCommandMiddlewareConfiguration(int parameter)
        {
            Parameter = parameter;
        }

        public int Parameter { get; set; }
    }

    private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestCommandMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.Configurations.Add(ctx.Configuration);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<TestCommandMiddlewareConfiguration> Configurations { get; } = new();
    }
}
