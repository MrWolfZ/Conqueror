namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

[TestFixture]
public sealed class RetryCommandMiddlewareTests
{
    [Test]
    public async Task GivenHandlerThatThrowsOnceWithDefaultConfiguration_WhenExecutingCommand_NoExceptionIsThrown()
    {
        var executionCount = 0;

        await using var serviceProvider = BuildServiceProvider(cmd =>
        {
            executionCount += 1;

            if (executionCount > 1)
            {
                return Task.FromResult(new TestCommandResponse(cmd.Parameter));
            }

            throw new InvalidOperationException("test exception");
        });

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(new(1)));
    }

    [Test]
    public async Task GivenHandlerThatContinuouslyThrowsWithDefaultConfiguration_WhenExecutingCommand_ExceptionIsThrown()
    {
        var expectedException = new InvalidOperationException("test exception");
        await using var serviceProvider = BuildServiceProvider(_ => throw expectedException);

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(1)));
        Assert.That(thrownException, Is.SameAs(expectedException));
    }

    [Test]
    public async Task GivenHandlerThatThrowsThreeTimesWithCustomRetryAttemptLimitOfThree_WhenExecutingCommand_NoExceptionIsThrown()
    {
        var executionCount = 0;

        await using var serviceProvider = BuildServiceProvider(cmd =>
        {
            executionCount += 1;

            if (executionCount > 3)
            {
                return Task.FromResult(new TestCommandResponse(cmd.Parameter));
            }

            throw new InvalidOperationException("test exception");
        }, pipeline => pipeline.UseRetry(o => o.RetryAttemptLimit = 3));

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        Assert.DoesNotThrowAsync(() => handler.ExecuteCommand(new(1)));
    }

    [Test]
    public async Task GivenHandlerThatContinuouslyThrowsWithCustomRetryAttemptLimitOfThree_WhenExecutingCommand_ExceptionIsThrown()
    {
        var expectedException = new InvalidOperationException("test exception");
        await using var serviceProvider = BuildServiceProvider(_ => throw expectedException, pipeline => pipeline.UseRetry(o => o.RetryAttemptLimit = 3));

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(1)));
        Assert.That(thrownException, Is.SameAs(expectedException));
    }

    private sealed record TestCommand(int Parameter);

    private sealed record TestCommandResponse(int Value);

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
    {
        private readonly Func<TestCommand, Task<TestCommandResponse>> executeFn;

        public TestCommandHandler(Func<TestCommand, Task<TestCommandResponse>> executeFn)
        {
            this.executeFn = executeFn;
        }

        public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) =>
            pipeline.ServiceProvider.GetRequiredService<Action<ICommandPipelineBuilder>>()(pipeline);

        public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            return executeFn(command);
        }
    }

    private static ServiceProvider BuildServiceProvider(Func<TestCommand, Task<TestCommandResponse>> handlerExecuteFn,
                                                        Action<ICommandPipelineBuilder>? configurePipeline = null)
    {
        return new ServiceCollection().AddConquerorCQS()
                                      .AddTransient<RetryCommandMiddleware>()
                                      .AddTransient<TestCommandHandler>()

                                      // add the retry middleware's default configuration
                                      .AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 1 })

                                      // add the dynamic execution function so that it can be injected into the handler
                                      .AddSingleton(handlerExecuteFn)

                                      // add the dynamic pipeline configuration function so that it can be used in the handler
                                      .AddSingleton(configurePipeline ?? (pipeline => pipeline.UseRetry()))

                                      .FinalizeConquerorRegistrations()
                                      .BuildServiceProvider();
    }
}
