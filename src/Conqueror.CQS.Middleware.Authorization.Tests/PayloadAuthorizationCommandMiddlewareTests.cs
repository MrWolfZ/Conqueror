using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class PayloadAuthorizationCommandMiddlewareTests : TestBase
{
    private Func<TestCommand, TestCommandResponse> handlerFn = _ => new();
    private Action<ICommandPipeline<TestCommand, TestCommandResponse>> configurePipeline = _ => { };

    [Test]
    public async Task GivenSuccessfulAuthorizationCheck_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenSuccessfulAuthorizationCheck_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenSuccessfulAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenSuccessfulSyncAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Success());

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleSuccessfulAuthorizationChecks_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Success());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleSuccessfulAuthorizationChecks_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Success());

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleSuccessfulAuthorizationChecks_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Success());

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenFailedAuthorizationCheck_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenFailedAuthorizationCheck_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenFailedAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationPayloadAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        var result = ConquerorAuthorizationResult.Failure("test");

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(result));

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteCommand(new()));

        Assert.That(thrownException?.Result, Is.SameAs(result));
    }

    [Test]
    public void GivenFailedSyncAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationPayloadAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        var result = ConquerorAuthorizationResult.Failure("test");

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => result);

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteCommand(new()));

        Assert.That(thrownException?.Result, Is.SameAs(result));
    }

    [Test]
    public async Task GivenMultipleFailedAuthorizationChecks_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 1")))
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Failure("test 2"));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleFailedAuthorizationChecks_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 1")))
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Failure("test 2"));

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenMultipleFailedAuthorizationChecks_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationPayloadAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        var result1 = ConquerorAuthorizationResult.Failure("test 1");
        var result2 = ConquerorAuthorizationResult.Failure("test 2");

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(result1))
                                                .AddPayloadAuthorizationCheck((_, _) => result2);

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteCommand(new()));

        Assert.That(thrownException?.Result.FailureReasons, Is.EquivalentTo(result1.FailureReasons.Concat(result2.FailureReasons)));
    }

    [Test]
    public async Task GivenMixedSuccessfulAndFailedAuthorizationChecks_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Success())
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMixedSuccessfulAndFailedAuthorizationChecks_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => ConquerorAuthorizationResult.Success())
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenMixedSuccessfulAndFailedAuthorizationChecks_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationPayloadAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        var successResult = ConquerorAuthorizationResult.Success();
        var failureResult = ConquerorAuthorizationResult.Failure("test");

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => successResult)
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(failureResult));

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteCommand(new()));

        Assert.That(thrownException?.Result, Is.SameAs(failureResult));
    }

    [Test]
    public async Task GivenAddedAndThenRemovedMiddleware_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenAddedAndThenRemovedMiddleware_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenAddedAndThenRemovedMiddleware_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
                        async (command, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(command);
                        },
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;
}
