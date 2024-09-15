using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class OperationTypeAuthorizationCommandMiddlewareTests : TestBase
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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => ConquerorAuthorizationResult.Success());

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.Handle(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenFailedAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationTypeAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        _ = Assert.ThrowsAsync<ConquerorOperationTypeAuthorizationFailedException>(() => Handler.Handle(new()));
    }

    [Test]
    public void GivenFailedSyncAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationTypeAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => ConquerorAuthorizationResult.Failure("test"));

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        _ = Assert.ThrowsAsync<ConquerorOperationTypeAuthorizationFailedException>(() => Handler.Handle(new()));
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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutCommandTypeAuthorization();

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutCommandTypeAuthorization();

        using var d = ConquerorContext.SetCurrentPrincipal(new());

        var response = await Handler.Handle(testCommand);

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

        configurePipeline = pipeline => pipeline.UseCommandTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutCommandTypeAuthorization();

        using var d = ConquerorContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.Handle(testCommand);

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
