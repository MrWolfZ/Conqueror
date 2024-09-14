namespace Conqueror.CQS.Tests.QueryHandling;

public sealed class QueryMiddlewareLifetimeTests
{
    [Test]
    public async Task GivenHandlerWithMiddleware_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestQueryHandler2>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse>>();

        _ = await handler1.ExecuteQuery(new());
        _ = await handler2.ExecuteQuery(new());
        _ = await handler3.ExecuteQuery(new());
        _ = await handler4.ExecuteQuery(new());
        _ = await handler5.ExecuteQuery(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenHandlerWithClientMiddleware_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddleware>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithoutMiddleware2>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse>>();

        _ = await handler1.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler2.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler3.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler4.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler5.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
    }

    [Test]
    public async Task GivenHandlerWithClientAndHandlerMiddleware_WhenMiddlewareIsExecuted_ServiceProviderInContextIsFromHandlerResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddConquerorQueryHandler<TestQueryHandler2>()
                    .AddScoped<DependencyResolvedDuringMiddlewareExecution>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse>>();
        var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse>>();

        _ = await handler1.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler2.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler3.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler4.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());
        _ = await handler5.WithPipeline(p => p.Use(new TestQueryMiddleware())).ExecuteQuery(new());

        Assert.That(observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 1, 2, 3, 4 }));
    }

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record TestQuery2;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => pipeline.Use(new TestQueryMiddleware());
    }

    private sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery2, TestQueryResponse> pipeline) => pipeline.Use(new TestQueryMiddleware());
    }

    private sealed class TestQueryHandlerWithoutMiddleware : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }
    }

    private sealed class TestQueryHandlerWithoutMiddleware2 : IQueryHandler<TestQuery2, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new();
        }
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();

            ctx.ServiceProvider.GetService<DependencyResolvedDuringMiddlewareExecution>()?.Execute();

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class DependencyResolvedDuringMiddlewareExecution(TestObservations observations)
    {
        private int invocationCount;

        public void Execute()
        {
            invocationCount += 1;
            observations.DependencyResolvedDuringMiddlewareExecutionInvocationCounts.Add(invocationCount);
        }
    }

    private sealed class TestObservations
    {
        public List<int> DependencyResolvedDuringMiddlewareExecutionInvocationCounts { get; } = new();
    }
}
