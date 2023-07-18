using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class PayloadAuthorizationCommandMiddlewareTests : TestBase
{
    private Func<TestCommand, TestCommandResponse> handlerFn = _ => new();
    private Action<ICommandPipelineBuilder> configurePipeline = _ => { };

    private IConquerorAuthenticationContext AuthenticationContext => Resolve<IConquerorAuthenticationContext>();

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Success());

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Success());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Success());

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Success());

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(result));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => result);

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 1")))
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Failure("test 2"));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 1")))
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Failure("test 2"));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(result1))
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => result2);

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Success())
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => ConquerorAuthorizationResult.Success())
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => successResult)
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(failureResult));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

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
                                                .AddPayloadAuthorizationCheck<TestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenAuthorizationCheckForBaseCommandType_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new SubTestCommand();
        var expectedResponse = new TestCommandResponse();

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<BaseTestCommand>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Resolve<ICommandHandler<SubTestCommand, TestCommandResponse>>().ExecuteCommand(testCommand);

        Assert.That(response, Is.EqualTo(expectedResponse));
    }

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCommonMiddlewareAuthentication()
                    .AddConquerorCQSAuthorizationMiddlewares()
                    .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
                        async (command, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(command);
                        },
                        pipeline => configurePipeline(pipeline))
                    .AddConquerorCommandHandlerDelegate<SubTestCommand, TestCommandResponse>(
                        async (_, _, _) =>
                        {
                            await Task.Yield();
                            return new();
                        },
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed record SubTestCommand : BaseTestCommand;

    private abstract record BaseTestCommand;
}
