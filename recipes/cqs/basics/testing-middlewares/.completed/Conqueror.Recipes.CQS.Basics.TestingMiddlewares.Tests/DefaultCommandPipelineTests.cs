namespace Conqueror.Recipes.CQS.Basics.TestingMiddlewares.Tests;

[TestFixture]
public sealed class DefaultCommandPipelineTests : TestBase
{
    private Action<ICommandPipelineBuilder> configurePipeline = pipeline => pipeline.UseDefault();

    private Func<TestCommand, Task<TestCommandResponse>> handlerExecutionFn = cmd => Task.FromResult(new TestCommandResponse(cmd.Parameter));

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    [Test]
    public void GivenHandlerWithDefaultPipeline_WhenExecutingWithInvalidCommand_ValidationExceptionIsThrown()
    {
        Assert.ThrowsAsync<ValidationException>(() => Handler.ExecuteCommand(new(-1)));
    }

    [Test]
    public void GivenHandlerThatThrowsOnceWithDefaultPipeline_WhenExecutingCommand_NoExceptionIsThrown()
    {
        var executionCount = 0;

        handlerExecutionFn = cmd =>
        {
            executionCount += 1;

            if (executionCount > 1)
            {
                return Task.FromResult(new TestCommandResponse(cmd.Parameter));
            }

            throw new InvalidOperationException("test exception");
        };

        Assert.DoesNotThrowAsync(() => Handler.ExecuteCommand(new(1)));
    }

    [Test]
    public void GivenHandlerThatThrowsTwiceWithDefaultPipelineAndCustomRetryConfiguration_WhenExecutingCommand_NoExceptionIsThrown()
    {
        var executionCount = 0;

        configurePipeline = pipeline => pipeline.UseDefault()
                                                .ConfigureRetry(o => o.RetryAttemptLimit = 2);

        handlerExecutionFn = cmd =>
        {
            executionCount += 1;

            if (executionCount > 2)
            {
                return Task.FromResult(new TestCommandResponse(cmd.Parameter));
            }

            throw new InvalidOperationException("test exception");
        };

        Assert.DoesNotThrowAsync(() => Handler.ExecuteCommand(new(1)));
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // create handler from delegate; note that we intentionally wrap the pipeline configuration function
        // in another arrow function to ensure it can the changed inside tests
        services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((command, _, _) => handlerExecutionFn(command),
                                                                                      pipeline => configurePipeline(pipeline));
    }

    private sealed record TestCommand(int Parameter)
    {
        [Range(1, int.MaxValue)]
        public int Parameter { get; } = Parameter;
    }

    private sealed record TestCommandResponse(int Value);
}
