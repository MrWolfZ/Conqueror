using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class OperationTypeAuthorizationQueryMiddlewareTests : TestBase
{
    private Func<TestQuery, TestQueryResponse> handlerFn = _ => new();
    private Action<IQueryPipeline<TestQuery, TestQueryResponse>> configurePipeline = _ => { };

    private static ConquerorAuthenticationContext AuthenticationContext => new();

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

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenSuccessfulSyncAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => ConquerorAuthorizationResult.Success());

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

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

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

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

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

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        _ = Assert.ThrowsAsync<ConquerorOperationTypeAuthorizationFailedException>(() => Handler.ExecuteQuery(new()));
    }

    [Test]
    public void GivenFailedSyncAuthorizationCheck_WhenExecutedWithAuthenticatedPrincipal_ThrowsOperationTypeAuthorizationFailedException()
    {
        handlerFn = _ =>
        {
            Assert.Fail("handler should not be executed");
            return new();
        };

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => ConquerorAuthorizationResult.Failure("test"));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        _ = Assert.ThrowsAsync<ConquerorOperationTypeAuthorizationFailedException>(() => Handler.ExecuteQuery(new()));
    }

    [Test]
    public async Task GivenAddedAndThenRemovedMiddleware_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutQueryTypeAuthorization();

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenAddedAndThenRemovedMiddleware_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutQueryTypeAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenAddedAndThenRemovedMiddleware_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UseQueryTypeAuthorization((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutQueryTypeAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    private IQueryHandler<TestQuery, TestQueryResponse> Handler => Resolve<IQueryHandler<TestQuery, TestQueryResponse>>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(
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
