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
            _ = pipeline.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestQueryMiddleware<TestQuery, TestQueryResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
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
            _ = pipeline.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestQueryMiddleware<TestQuery, TestQueryResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20, 40, 60 }));
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
            _ = pipeline.Use(new TestQueryMiddlewareSub<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestQueryMiddlewareBase<TestQuery, TestQueryResponse>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
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
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestQueryMiddleware<TestQuery, TestQueryResponse>>(c => c.Parameter += 10));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new(), CancellationToken.None);
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestQueryMiddleware<TQuery, TResponse>(TestObservations observations) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryMiddlewareSub<TQuery, TResponse>(TestObservations observations) : TestQueryMiddlewareBase<TQuery, TResponse>(observations)
        where TQuery : class;

    private abstract class TestQueryMiddlewareBase<TQuery, TResponse>(TestObservations observations) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public int Parameter { get; set; }

        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
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
