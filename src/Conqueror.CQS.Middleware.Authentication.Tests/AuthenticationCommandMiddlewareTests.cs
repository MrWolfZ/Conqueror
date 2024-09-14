using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authentication.Tests;

[TestFixture]
public sealed class AuthenticationCommandMiddlewareTests : TestBase
{
    private Func<TestCommand, TestCommandResponse> handlerFn = _ => new();
    private Action<ICommandPipeline<TestCommand, TestCommandResponse>> configurePipeline = _ => { };

    private static ConquerorAuthenticationContext AuthenticationContext => new();

    [Test]
    public async Task GivenDefaultConfiguration_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication();

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenDefaultConfiguration_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenDefaultConfiguration_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenConfigurationAllowingAnonymousAccess_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess();

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenConfigurationAllowingAnonymousAccess_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenConfigurationAllowingAnonymousAccess_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithoutPrincipal_ThrowsMissingPrincipalException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal();

        _ = Assert.ThrowsAsync<ConquerorAuthenticationMissingPrincipalException>(() => Handler.ExecuteCommand(new()));
    }

    [Test]
    public void GivenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithUnauthenticatedPrincipal_ThrowsUnauthenticatedPrincipalException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        _ = Assert.ThrowsAsync<ConquerorAuthenticationUnauthenticatedPrincipalException>(() => Handler.ExecuteCommand(new()));
    }

    [Test]
    public void GivenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithRemovedAuthenticatedPrincipal_ThrowsMissingPrincipalException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        using var d2 = AuthenticationContext.SetCurrentPrincipal(null);

        _ = Assert.ThrowsAsync<ConquerorAuthenticationMissingPrincipalException>(() => Handler.ExecuteCommand(new()));
    }

    [Test]
    public async Task GivenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenOverriddenConfigurationAllowingAnonymousAccess_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().AllowAnonymousAccess();

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenOverriddenConfigurationAllowingAnonymousAccess_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenOverriddenConfigurationAllowingAnonymousAccess_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenOverriddenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithoutPrincipal_ThrowsMissingPrincipalException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess().RequireAuthenticatedPrincipal();

        _ = Assert.ThrowsAsync<ConquerorAuthenticationMissingPrincipalException>(() => Handler.ExecuteCommand(new()));
    }

    [Test]
    public void GivenOverriddenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithUnauthenticatedPrincipal_ThrowsUnauthenticatedPrincipalException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        _ = Assert.ThrowsAsync<ConquerorAuthenticationUnauthenticatedPrincipalException>(() => Handler.ExecuteCommand(new()));
    }

    [Test]
    public async Task GivenOverriddenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedAuthenticationMiddleware_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().WithoutAuthentication();

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedAuthenticationMiddleware_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().WithoutAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedAuthenticationMiddleware_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testCommand = new TestCommand();
        var expectedResponse = new TestCommandResponse();

        handlerFn = cmd =>
        {
            Assert.That(cmd, Is.SameAs(testCommand));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().WithoutAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteCommand(testCommand);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    private ICommandHandler<TestCommand, TestCommandResponse> Handler => Resolve<ICommandHandler<TestCommand, TestCommandResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSAuthenticationMiddlewares()
                    .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(
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
