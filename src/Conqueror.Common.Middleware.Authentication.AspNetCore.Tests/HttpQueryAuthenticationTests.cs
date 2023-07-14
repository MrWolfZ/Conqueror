using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Conqueror.Common.Middleware.Authentication.AspNetCore.Tests;

[TestFixture]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "necessary for dynamic controller generation")]
public sealed class HttpQueryAuthenticationTests : TestBase
{
    private const string TestUserId = "QueryAuthenticationTestUser";

    private bool requestIsAuthenticated;
    private Exception? exceptionToThrow;
    private ClaimsPrincipal? observedPrincipal;

    [Test]
    public async Task GivenHandler_WhenExecutingWithoutAuthenticatedPrincipal_HandlerObservesUnauthenticatedPrincipal()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        var result = await handler.ExecuteQuery(new() { Payload = 10 });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Payload, Is.EqualTo(11));

        Assert.That(observedPrincipal?.Identity?.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task GivenHandler_WhenExecutingWithAuthenticatedPrincipal_HandlerObservesCorrectPrincipal()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        requestIsAuthenticated = true;

        var result = await handler.ExecuteQuery(new() { Payload = 10 });

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Payload, Is.EqualTo(11));

        Assert.That(observedPrincipal, Is.Not.Null);
        Assert.That(observedPrincipal?.Identity?.Name, Is.EqualTo(TestUserId));
    }

    [Test]
    public async Task GivenHandlerThatThrowsMissingPrincipalException_WhenExecutingWithoutAuthenticatedPrincipal_RequestFailsWithUnauthorized()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        exceptionToThrow = new ConquerorAuthenticationMissingPrincipalException("test");

        var thrownException = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new() { Payload = 10 }));

        await thrownException!.Response!.AssertStatusCode(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GivenHandlerThatThrowsUnauthenticatedPrincipalException_WhenExecutingWithoutAuthenticatedPrincipal_RequestFailsWithUnauthorized()
    {
        var handler = ResolveOnClient<ITestQueryHandler>();

        exceptionToThrow = new ConquerorAuthenticationUnauthenticatedPrincipalException("test");

        var thrownException = Assert.ThrowsAsync<HttpQueryFailedException>(() => handler.ExecuteQuery(new() { Payload = 10 }));

        await thrownException!.Response!.AssertStatusCode(HttpStatusCode.Unauthorized);
    }

    protected override void ConfigureServerServices(IServiceCollection services)
    {
        _ = services.AddMvc().AddConquerorCQSHttpControllers();

        _ = services.AddConquerorCommonMiddlewareAuthentication();
        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (qry, p, _) =>
        {
            await Task.Yield();

            observedPrincipal = p.GetRequiredService<IConquerorAuthenticationContext>().GetCurrentPrincipal();

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

        _ = services.AddConquerorQueryClient<ITestQueryHandler>(b => b.UseHttp(baseAddress, o =>
        {
            if (requestIsAuthenticated)
            {
                WithAuthenticatedPrincipal(o.Headers, TestUserId);
            }
        }));
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
        _ = app.UseConquerorAuthentication();

        _ = app.UseRouting();
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
