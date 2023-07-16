using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class FunctionalAuthorizationCommandMiddlewareTests : TestBase
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

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

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

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenFailedAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_ThrowsFunctionalAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        _ = Assert.ThrowsAsync<ConquerorFunctionalAuthorizationFailedException>(() => Handler.ExecuteCommand(new()));
    }

    [Test]
    public async Task GivenRemovedFunctionalAuthorizationMiddleware_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutFunctionalAuthorization();

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedFunctionalAuthorizationMiddleware_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutFunctionalAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedFunctionalAuthorizationMiddleware_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutFunctionalAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
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
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;
}
