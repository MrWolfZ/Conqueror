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

    private static ServiceProvider BuildServiceProvider(Func<TestCommand, Task<TestCommandResponse>> handlerExecuteFn,
                                                        Action<ICommandPipeline<TestCommand, TestCommandResponse>>? configurePipeline = null)
    {
        return new ServiceCollection().AddConquerorCommandMiddleware<RetryCommandMiddleware>()

                                      // create a handler from a delegate
                                      .AddConquerorCommandHandlerDelegate((command, _, _) => handlerExecuteFn(command),
                                                                          configurePipeline ?? (pipeline => pipeline.UseRetry()))

                                      // add the retry middleware's default configuration
                                      .AddSingleton(new RetryMiddlewareConfiguration { RetryAttemptLimit = 1 })

                                      .BuildServiceProvider();
    }
}
