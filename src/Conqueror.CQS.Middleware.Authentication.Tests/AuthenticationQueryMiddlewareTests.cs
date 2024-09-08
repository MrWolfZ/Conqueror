using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authentication.Tests;

[TestFixture]
public sealed class AuthenticationQueryMiddlewareTests : TestBase
{
    private Func<TestQuery, TestQueryResponse> handlerFn = _ => new();
    private Action<IQueryPipeline<TestQuery, TestQueryResponse>> configurePipeline = _ => { };

    private IConquerorAuthenticationContext AuthenticationContext => Resolve<IConquerorAuthenticationContext>();

    [Test]
    public async Task GivenDefaultConfiguration_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication();

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenDefaultConfiguration_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenDefaultConfiguration_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenConfigurationAllowingAnonymousAccess_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess();

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenConfigurationAllowingAnonymousAccess_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenConfigurationAllowingAnonymousAccess_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

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

        _ = Assert.ThrowsAsync<ConquerorAuthenticationMissingPrincipalException>(() => Handler.ExecuteQuery(new()));
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

        _ = Assert.ThrowsAsync<ConquerorAuthenticationUnauthenticatedPrincipalException>(() => Handler.ExecuteQuery(new()));
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

        _ = Assert.ThrowsAsync<ConquerorAuthenticationMissingPrincipalException>(() => Handler.ExecuteQuery(new()));
    }

    [Test]
    public async Task GivenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenOverriddenConfigurationAllowingAnonymousAccess_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().AllowAnonymousAccess();

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenOverriddenConfigurationAllowingAnonymousAccess_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenOverriddenConfigurationAllowingAnonymousAccess_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().AllowAnonymousAccess();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

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

        _ = Assert.ThrowsAsync<ConquerorAuthenticationMissingPrincipalException>(() => Handler.ExecuteQuery(new()));
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

        _ = Assert.ThrowsAsync<ConquerorAuthenticationUnauthenticatedPrincipalException>(() => Handler.ExecuteQuery(new()));
    }

    [Test]
    public async Task GivenOverriddenConfigurationWithAuthenticatedPrincipalRequirement_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().AllowAnonymousAccess().RequireAuthenticatedPrincipal();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedAuthenticationMiddleware_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().WithoutAuthentication();

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedAuthenticationMiddleware_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().WithoutAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedAuthenticationMiddleware_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseAuthentication().RequireAuthenticatedPrincipal().WithoutAuthentication();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCQSAuthenticationMiddlewares()
                    .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(
                        async (query, _, _) =>
                        {
                            await Task.Yield();
                            return handlerFn(query);
                        },
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;
}
