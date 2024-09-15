namespace Conqueror.CQS.Tests.CommandHandling;

public sealed class CommandMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenHandlerWithMiddleware_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandHandler2>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse>>();

        _ = await handler1.ExecuteCommand(new());
        _ = await handler2.ExecuteCommand(new());
        _ = await handler3.ExecuteCommand(new());
        _ = await handler4.ExecuteCommand(new());
        _ = await handler5.ExecuteCommand(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenHandlerWithClientMiddleware_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutMiddleware>()
                    .AddConquerorCommandHandler<TestCommandHandlerWithoutMiddleware2>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse>>();

        _ = await handler1.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler2.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler3.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand2, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler4.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler5.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand2, TestCommandResponse>())).ExecuteCommand(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenHandlerWithClientAndHandlerMiddleware_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddConquerorCommandHandler<TestCommandHandler2>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse>>();

        _ = await handler1.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler2.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler3.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand2, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler4.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>())).ExecuteCommand(new());
        _ = await handler5.WithPipeline(p => p.Use(new TestCommandMiddleware<TestCommand2, TestCommandResponse>())).ExecuteCommand(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 1, 2, 3, 4 }));
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed record TestCommand2;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestCommandResponse> pipeline) => pipeline.Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>());
    }

    private sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand2, TestCommandResponse> pipeline) => pipeline.Use(new TestCommandMiddleware<TestCommand2, TestCommandResponse>());
    }

    private sealed class TestCommandHandlerWithoutMiddleware : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }
    }

    private sealed class TestCommandHandlerWithoutMiddleware2 : ICommandHandler<TestCommand2, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }
    }

    private sealed class TestCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class DependencyResolvedDuringMiddlewareExecution(TestObservations observations)
    {
        private int invocationCount;

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = [];
    }
}
