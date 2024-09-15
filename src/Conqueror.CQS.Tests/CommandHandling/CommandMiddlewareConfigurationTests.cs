namespace Conqueror.CQS.Tests.CommandHandling;

public sealed class CommandMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline =>
        {
            _ = pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestCommandMiddleware<TestCommand, TestCommandResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline =>
        {
            _ = pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestCommandMiddleware<TestCommand, TestCommandResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20, 40, 60 }));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline =>
        {
            _ = pipeline.Use(new TestCommandMiddlewareSub<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestCommandMiddlewareBase<TestCommand, TestCommandResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenUnusedMiddleware_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestCommandMiddleware<TestCommand, TestCommandResponse>>(c => c.Parameter += 10));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.Handle(new(), CancellationToken.None);
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestCommandResponse> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<ICommandPipeline<TestCommand, TestCommandResponse>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestCommandMiddleware<TCommand, TResponse>(TestObservations observations) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandMiddlewareSub<TCommand, TResponse>(TestObservations observations) : TestCommandMiddlewareBase<TCommand, TResponse>(observations)
        where TCommand : class;

    private abstract class TestCommandMiddlewareBase<TCommand, TResponse>(TestObservations observations) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }
}
