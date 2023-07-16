using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class FunctionalAuthorizationQueryMiddlewareTests : TestBase
{
    private Func<TestQuery, TestQueryResponse> handlerFn = _ => new();
    private Action<IQueryPipelineBuilder> configurePipeline = _ => { };

    private IConquerorAuthenticationContext AuthenticationContext => Resolve<IConquerorAuthenticationContext>();

    [Test]
    public async Task GivenSuccessfulAuthorizationCheck_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenSuccessfulAuthorizationCheck_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenSuccessfulAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenFailedAuthorizationCheck_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenFailedAuthorizationCheck_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

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

        _ = Assert.ThrowsAsync<ConquerorFunctionalAuthorizationFailedException>(() => Handler.ExecuteQuery(new()));
    }

    [Test]
    public async Task GivenRemovedFunctionalAuthorizationMiddleware_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutFunctionalAuthorization();

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedFunctionalAuthorizationMiddleware_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutFunctionalAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenRemovedFunctionalAuthorizationMiddleware_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseFunctionalAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutFunctionalAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorCommonMiddlewareAuthentication()
                    .AddConquerorCQSAuthorizationMiddlewares()
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
