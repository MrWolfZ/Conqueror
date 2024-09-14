using Polly;

namespace Conqueror.CQS.Middleware.Polly.Tests;

[TestFixture]
public sealed class PollyCommandMiddlewareTests : TestBase
{
    private Func<TestCommand, TestCommandResponse> handlerFn = _ => new();
    private Action<ICommandPipeline<TestCommand, TestCommandResponse>> configurePipeline = _ => { };

    [Test]
    public async Task GivenDefaultMiddlewareConfiguration_ExecutesHandlerWithoutModification()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.Use(new PollyCommandMiddleware<TestCommand, TestCommandResponse> { Configuration = new() });

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenDefaultMiddlewareConfiguration_ExecutesThrowingHandlerWithoutModification()
    {
        var testCommand = new TestCommand();
        var expectedException = new InvalidOperationException();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            throw expectedException;
        };

        configurePipeline = pipeline => pipeline.Use(new PollyCommandMiddleware<TestCommand, TestCommandResponse> { Configuration = new() });

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        Assert.That(thrownException, Is.SameAs(expectedException));
    }

    [Test]
    public async Task GivenConfigurationWithPolicy_ExecutesHandlerWithPolicy()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        var executionCount = 0;

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));

            executionCount += 1;

            if (executionCount < 2)
            {
                throw new InvalidOperationException();
            }

            return expectedResponse;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

        configurePipeline = pipeline => pipeline.UsePolly(policy);

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
        Assert.That(executionCount, Is.EqualTo(2));
    }

    [Test]
    public void GivenConfigurationWithPolicy_ExecutesThrowingHandlerWithPolicy()
    {
        var testCommand = new TestCommand();
        var expectedException = new InvalidOperationException();

        var executionCount = 0;

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));

            executionCount += 1;

            throw expectedException;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync(3);

        configurePipeline = pipeline => pipeline.UsePolly(policy);

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(executionCount, Is.EqualTo(4));
    }

    [Test]
    public async Task GivenOverriddenConfigurationWithPolicy_ExecutesHandlerWithOverriddenPolicy()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        var executionCount = 0;

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));

            executionCount += 1;

            if (executionCount < 2)
            {
                throw new InvalidOperationException();
            }

            return expectedResponse;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

        configurePipeline = pipeline => pipeline.UsePolly(Policy.NoOpAsync())
                                                .ConfigurePollyPolicy(policy);

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
        Assert.That(executionCount, Is.EqualTo(2));
    }

    [Test]
    public void GivenOverriddenConfigurationWithPolicy_ExecutesThrowingHandlerWithOverriddenPolicy()
    {
        var testCommand = new TestCommand();
        var expectedException = new InvalidOperationException();

        var executionCount = 0;

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));

            executionCount += 1;

            throw expectedException;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync(3);

        configurePipeline = pipeline => pipeline.UsePolly(Policy.NoOpAsync())
                                                .ConfigurePollyPolicy(policy);

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(executionCount, Is.EqualTo(4));
    }

    [Test]
    public void GivenRemovedPollyMiddleware_ExecutesHandlerWithoutModification()
    {
        var testCommand = new TestCommand();
        var expectedException = new InvalidOperationException();

        var executionCount = 0;

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));

            executionCount += 1;

            if (executionCount < 2)
            {
                throw expectedException;
            }

            return new();
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

        configurePipeline = pipeline => pipeline.UsePolly(policy)
                                                .WithoutPolly();

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteCommand(testCommand));

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(executionCount, Is.EqualTo(1));
    }

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
                        async (query, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(query);
                        },
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;
}
