using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Conqueror.Common.Middleware.Authorization.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class HttpCommandAuthorizationTests : TestBase
{
    private const string TestUserId = "CommandAuthorizationTestUser";

    private bool handlerWasCalled;
    private Exception? exceptionToThrow;
    private string userId = TestUserId;

    [Test]
    public async Task GivenHandlerThatThrowsFunctionalAuthorizationFailedException_WhenExecutingCommand_RequestFailsWithForbidden()
    {
        var handler = ResolveOnClient<ITestCommandHandler>();

        exceptionToThrow = new ConquerorFunctionalAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        var thrownException = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }));

        await thrownException!.Response!.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenHandlerThatThrowsDataAuthorizationFailedException_WhenExecutingCommand_RequestFailsWithForbidden()
    {
        var handler = ResolveOnClient<ITestCommandHandler>();

        exceptionToThrow = new ConquerorDataAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        var thrownException = Assert.ThrowsAsync<HttpCommandFailedException>(() => handler.ExecuteCommand(new() { Payload = 10 }));

        await thrownException!.Response!.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenAspDefaultAuthorizationPolicy_WhenExecutingCommandWithUserWhichWouldFailAspAuthorization_RequestIsPassedThroughToHandler()
    {
        var handler = ResolveOnClient<ITestCommandHandler>();

        userId = "unauthorized_user";

        var result = await handler.ExecuteCommand(new() { Payload = 10 });
        
        Assert.That(result, Is.Not.Null);
        Assert.That(handlerWasCalled, Is.True);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder().RequireUserName(TestUserId).Build());

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(async (cmd, _, _) =>
        {
            await Task.Yield();

            handlerWasCalled = true;

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            return new() { Payload = cmd.Payload + 1 };
        });
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSHttpClientServices(o => { _ = o.UseHttpClient(HttpClient); });

        var baseAddress = new Uri("http://conqueror.test");

        _ = services.AddConquerorCommandClient<ITestCommandHandler>(b => b.UseHttp(baseAddress, o => WithAuthenticatedPrincipal(o.Headers, userId)));
    }

    protected override void Configure(IApplicationBuilder app)
    {
        _ = app.Use(async (ctx, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception)
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        });

        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.UseConquerorAuthorization();

        _ = app.UseRouting();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpCommand]
    public sealed record TestCommand
    {
        public int Payload { get; init; }
    }

    public sealed record TestCommandResponse
    {
        public int Payload { get; init; }
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }
}
