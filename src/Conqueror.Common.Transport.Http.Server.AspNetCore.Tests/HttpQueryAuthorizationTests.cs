using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Conqueror.Common.Transport.Http.Server.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class HttpQueryAuthorizationTests : TestBase
{
    private const string TestUserId = "QueryAuthorizationTestUser";

    private bool handlerWasCalled;
    private Exception? exceptionToThrow;
    private string userId = TestUserId;

    [Test]
    public async Task GivenHandlerThatThrowsOperationTypeAuthorizationFailedException_WhenExecutingQuery_RequestFailsWithForbidden()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        exceptionToThrow = new ConquerorOperationTypeAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        var thrownException = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new() { Payload = 10 }));

        await thrownException!.Response!.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenHandlerThatThrowsOperationPayloadAuthorizationFailedException_WhenExecutingQuery_RequestFailsWithForbidden()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        exceptionToThrow = new ConquerorOperationPayloadAuthorizationFailedException("test", ConquerorAuthorizationResult.Failure("test"));

        var thrownException = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new() { Payload = 10 }));

        await thrownException!.Response!.AssertStatusCode(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GivenAspDefaultAuthorizationPolicy_WhenExecutingQueryWithUserWhichWouldFailAspAuthorization_RequestIsPassedThroughToHandler()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        userId = "unauthorized_user";

        var result = await handler.ExecuteQuery(new() { Payload = 10 });
        
        Assert.That(result, Is.Not.Null);
        Assert.That(handlerWasCalled, Is.True);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder().RequireUserName(TestUserId).Build());

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (qry, _, _) =>
        {
            await Task.Yield();

            handlerWasCalled = true;

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            return new() { Payload = qry.Payload + 1 };
        });
    }

    protected override void ConfigureClientServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSHttpClientServices(o => { _ = o.UseHttpClient(HttpClient); });

        var baseAddress = new Uri("http://conqueror.test");

        _ = services.AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(baseAddress, o => WithAuthenticatedPrincipal(o.Headers, userId)));
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

        _ = app.UseRouting();
        _ = app.UseConqueror();
        _ = app.UseEndpoints(b => b.MapControllers());
    }

    [HttpQuery]
    public sealed record TestQuery
    {
        public int Payload { get; init; }
    }

    public sealed record TestQueryResponse
    {
        public int Payload { get; init; }
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }
}
