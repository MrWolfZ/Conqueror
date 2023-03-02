namespace Conqueror.CQS.Tests;

[TestFixture]
public sealed class QueryServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>();

        Assert.DoesNotThrow(() => services.AddConquerorQueryHandler<TestQueryHandler>());
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<TestQueryHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameQueryAndResponseTypesKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                              .AddConquerorQueryHandler<DuplicateTestQueryHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestQueryHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IQueryHandler<TestQuery, TestQueryResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameQueryTypeAndDifferentResponseTypeThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryHandler<DuplicateTestQueryHandlerWithDifferentResponseType>());
    }

    [Test]
    public void GivenHandlerTypeWithInstanceFactory_AddedHandlerCanBeResolvedFromInterface()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler(_ => new TestQueryHandler())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenMiddlewareTypeWithInstanceFactory_AddedMiddlewareCanBeUsedInPipeline()
    {
        var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithMiddleware>()
                                              .AddConquerorQueryMiddleware(_ => new TestQueryMiddleware())
                                              .BuildServiceProvider();

        Assert.DoesNotThrowAsync(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(new(), CancellationToken.None));
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record TestQueryResponse2;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestQueryHandlerWithDifferentResponseType : IQueryHandler<TestQuery, TestQueryResponse2>
    {
        public Task<TestQueryResponse2> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestQueryHandlerWithMiddleware : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.Use<TestQueryMiddleware>();
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }
}
