using Polly;

namespace Conqueror.CQS.Middleware.Polly.Tests;

[TestFixture]
public sealed class PollyQueryMiddlewareTests : TestBase
{
    private Func<TestQuery, TestQueryResponse> handlerFn = _ => new();
    private Action<IQueryPipeline<TestQuery, TestQueryResponse>> configurePipeline = _ => { };

    [Test]
    public async Task GivenDefaultMiddlewareConfiguration_ExecutesHandlerWithoutModification()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));
            return expectedResponse;
        };

        configurePipeline = pipeline => pipeline.Use(new PollyQueryMiddleware<TestQuery, TestQueryResponse> { Configuration = new() });

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
    }

    [Test]
    public void GivenDefaultMiddlewareConfiguration_ExecutesThrowingHandlerWithoutModification()
    {
        var testQuery = new TestQuery();
        var expectedException = new InvalidOperationException();

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));
            throw expectedException;
        };

        configurePipeline = pipeline => pipeline.Use(new PollyQueryMiddleware<TestQuery, TestQueryResponse> { Configuration = new() });

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        Assert.That(thrownException, Is.SameAs(expectedException));
    }

    [Test]
    public async Task GivenConfigurationWithPolicy_ExecutesHandlerWithPolicy()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        var executionCount = 0;

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));

            executionCount += 1;

            if (executionCount < 2)
            {
                throw new InvalidOperationException();
            }

            return expectedResponse;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

        configurePipeline = pipeline => pipeline.UsePolly(policy);

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
        Assert.That(executionCount, Is.EqualTo(2));
    }

    [Test]
    public void GivenConfigurationWithPolicy_ExecutesThrowingHandlerWithPolicy()
    {
        var testQuery = new TestQuery();
        var expectedException = new InvalidOperationException();

        var executionCount = 0;

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));

            executionCount += 1;

            throw expectedException;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync(3);

        configurePipeline = pipeline => pipeline.UsePolly(policy);

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(executionCount, Is.EqualTo(4));
    }

    [Test]
    public async Task GivenOverriddenConfigurationWithPolicy_ExecutesHandlerWithOverriddenPolicy()
    {
        var testQuery = new TestQuery();
        var expectedResponse = new TestQueryResponse();

        var executionCount = 0;

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));

            executionCount += 1;

            if (executionCount < 2)
            {
                throw new InvalidOperationException();
            }

            return expectedResponse;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

        configurePipeline = pipeline => pipeline.UsePolly(Policy.NoOpAsync())
                                                .ConfigurePollyPolicy(policy);

        var response = await Handler.ExecuteQuery(testQuery);

        Assert.That(response, Is.SameAs(expectedResponse));
        Assert.That(executionCount, Is.EqualTo(2));
    }

    [Test]
    public void GivenOverriddenConfigurationWithPolicy_ExecutesThrowingHandlerWithOverriddenPolicy()
    {
        var testQuery = new TestQuery();
        var expectedException = new InvalidOperationException();

        var executionCount = 0;

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));

            executionCount += 1;

            throw expectedException;
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync(3);

        configurePipeline = pipeline => pipeline.UsePolly(Policy.NoOpAsync())
                                                .ConfigurePollyPolicy(policy);

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(executionCount, Is.EqualTo(4));
    }

    [Test]
    public void GivenRemovedPollyMiddleware_ExecutesHandlerWithoutModification()
    {
        var testQuery = new TestQuery();
        var expectedException = new InvalidOperationException();

        var executionCount = 0;

        handlerFn = query =>
        {
            Assert.That(query, Is.SameAs(testQuery));

            executionCount += 1;

            if (executionCount < 2)
            {
                throw expectedException;
            }

            return new();
        };

        var policy = Policy.Handle<InvalidOperationException>().RetryAsync();

        configurePipeline = pipeline => pipeline.UsePolly(policy)
                                                .WithoutPolly();

        var thrownException = Assert.ThrowsAsync<InvalidOperationException>(() => Handler.ExecuteQuery(testQuery));

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(executionCount, Is.EqualTo(1));
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
