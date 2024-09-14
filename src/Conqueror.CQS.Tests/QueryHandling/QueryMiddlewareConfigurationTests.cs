namespace Conqueror.CQS.Tests.QueryHandling;

public sealed class QueryMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline =>
        {
            _ = pipeline.Use(new TestQueryMiddleware(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestQueryMiddleware>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline =>
        {
            _ = pipeline.Use(new TestQueryMiddleware(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestQueryMiddleware(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestQueryMiddleware(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestQueryMiddleware>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20, 40, 60 }));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline =>
        {
            _ = pipeline.Use(new TestQueryMiddlewareSub(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestQueryMiddlewareBase>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenUnusedMiddleware_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestQueryMiddleware>(c => c.Parameter += 10));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(), CancellationToken.None);
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestQueryMiddleware(TestObservations observations) : IQueryMiddleware
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryMiddlewareSub(TestObservations observations) : TestQueryMiddlewareBase(observations);

    private abstract class TestQueryMiddlewareBase(TestObservations observations) : IQueryMiddleware
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }
}
