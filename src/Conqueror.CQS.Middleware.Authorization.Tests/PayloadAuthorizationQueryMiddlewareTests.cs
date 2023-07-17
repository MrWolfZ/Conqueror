using System.Security.Claims;

namespace Conqueror.CQS.Middleware.Authorization.Tests;

[TestFixture]
public sealed class PayloadAuthorizationQueryMiddlewareTests : TestBase
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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleSuccessfulAuthorizationChecks_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleSuccessfulAuthorizationChecks_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleSuccessfulAuthorizationChecks_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

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
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(result));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteQuery(new()));

        Assert.That(thrownException?.Result, Is.SameAs(result));
    }

    [Test]
    public async Task GivenMultipleFailedAuthorizationChecks_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 1")))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 2")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMultipleFailedAuthorizationChecks_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 1")))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test 2")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

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
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(result1))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(result2));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteQuery(new()));

        Assert.That(thrownException?.Result.FailureReasons, Is.EquivalentTo(result1.FailureReasons.Concat(result2.FailureReasons)));
    }

    [Test]
    public async Task GivenMixedSuccessfulAndFailedAuthorizationChecks_WhenExecutedWithoutPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenMixedSuccessfulAndFailedAuthorizationChecks_WhenExecutedWithUnauthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = qry =>
        {
            Assert.That(qry, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")));

        using var d = AuthenticationContext.SetCurrentPrincipal(new());

        var response = await Handler.ExecuteQuery(testQuery);

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
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(successResult))
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(failureResult));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var thrownException = Assert.ThrowsAsync<ConquerorOperationPayloadAuthorizationFailedException>(() => Handler.ExecuteQuery(new()));

        Assert.That(thrownException?.Result, Is.SameAs(failureResult));
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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

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

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<TestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Failure("test")))
                                                .WithoutPayloadAuthorization();

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task GivenAuthorizationCheckForBaseQueryType_WhenExecutedWithAuthenticatedPrincipal_AllowsExecution()
    {
        var testQuery = new SubTestQuery();
        var expectedResponse = new TestQueryResponse();

        configurePipeline = pipeline => pipeline.UsePayloadAuthorization()
                                                .AddPayloadAuthorizationCheck<BaseTestQuery>((_, _) => Task.FromResult(ConquerorAuthorizationResult.Success()));

        using var d = AuthenticationContext.SetCurrentPrincipal(new(new ClaimsIdentity("test")));

        var response = await Resolve<IQueryHandler<SubTestQuery, TestQueryResponse>>().ExecuteQuery(testQuery);

        Assert.That(response, Is.EqualTo(expectedResponse));
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
                        pipeline => configurePipeline(pipeline))
                    .AddConquerorQueryHandlerDelegate<SubTestQuery, TestQueryResponse>(
                        async (_, _, _) =>
                        {
                            await Task.Yield();
                            return new();
                        },
                        pipeline => configurePipeline(pipeline));
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record SubTestQuery : BaseTestQuery;

    private abstract record BaseTestQuery;
}
