namespace Conqueror.CQS.Tests.QueryHandling;

public sealed class QueryIdTests
{
    [Test]
    public async Task GivenQueryExecution_QueryIdIsTheSameInHandlerAndMiddlewareAndNestedClass()
    {
        var query = new TestQuery(10);
        var observedQueryIds = new List<string?>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedQueryIds.Add(ctx!.GetQueryId());
                return new(cmd.Payload);
            },
            (cmd, _) => new(cmd.Payload),
            (ctx, next) =>
            {
                observedQueryIds.Add(ctx.ConquerorContext.GetQueryId());
                return next(ctx.Query);
            },
            (ctx, next) => next(ctx.Query),
            ctx => { observedQueryIds.Add(ctx!.GetQueryId()); });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedQueryIds, Has.Count.EqualTo(3));
        Assert.That(observedQueryIds[1], Is.SameAs(observedQueryIds[0]));
        Assert.That(observedQueryIds[2], Is.SameAs(observedQueryIds[0]));
    }

    [Test]
    public async Task GivenQueryExecution_QueryIdIsNotTheSameInNestedHandler()
    {
        var query = new TestQuery(10);
        var observedQueryIds = new List<string?>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedQueryIds.Add(ctx!.GetQueryId());
                return new(cmd.Payload);
            },
            (cmd, ctx) =>
            {
                observedQueryIds.Add(ctx!.GetQueryId());
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedQueryIds, Has.Count.EqualTo(2));
        Assert.That(observedQueryIds[1], Is.Not.SameAs(observedQueryIds[0]));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestQuery, ConquerorContext?, TestQueryResponse>? handlerFn = null,
                                   Func<NestedTestQuery, ConquerorContext?, NestedTestQueryResponse>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<ConquerorContext?>? nestedClassFn = null)
    {
        handlerFn ??= (cmd, _) => new(cmd.Payload);
        nestedHandlerFn ??= (cmd, _) => new(cmd.Payload);
        middlewareFn ??= (ctx, next) => next(ctx.Query);
        outerMiddlewareFn ??= (ctx, next) => next(ctx.Query);
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(p => new(handlerFn,
                                                                         p.GetRequiredService<IConquerorContextAccessor>(),
                                                                         p.GetRequiredService<NestedClass>(),
                                                                         p.GetRequiredService<IQueryHandler<NestedTestQuery, NestedTestQueryResponse>>()));

        _ = services.AddConquerorQueryHandler<NestedTestQueryHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()));

        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), middlewareFn);
        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(OuterTestQueryMiddleware<TestQuery, TestQueryResponse>), outerMiddlewareFn);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestQueryHandler>();

        return provider.CreateScope().ServiceProvider;
    }

    private delegate Task<TestQueryResponse> MiddlewareFn(QueryMiddlewareContext<TestQuery, TestQueryResponse> middlewareCtx,
                                                          Func<TestQuery, Task<TestQueryResponse>> next);

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int Payload);

    private sealed record NestedTestQuery(int Payload);

    private sealed record NestedTestQueryResponse(int Payload);

    private sealed class TestQueryHandler(
        Func<TestQuery, ConquerorContext?, TestQueryResponse> handlerFn,
        IConquerorContextAccessor conquerorContextAccessor,
        NestedClass nestedClass,
        IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler)
        : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(query, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedQueryHandler.ExecuteQuery(new(query.Payload), cancellationToken);
            return response;
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => pipeline.Use(new OuterTestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFn>(typeof(OuterTestQueryMiddleware<TestQuery, TestQueryResponse>))))
                                                                                                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFn>(typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>))));
    }

    private sealed class NestedTestQueryHandler(
        Func<NestedTestQuery, ConquerorContext?, NestedTestQueryResponse> handlerFn,
        IConquerorContextAccessor conquerorContextAccessor)
        : IQueryHandler<NestedTestQuery, NestedTestQueryResponse>
    {
        public async Task<NestedTestQueryResponse> ExecuteQuery(NestedTestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return handlerFn(query, conquerorContextAccessor.ConquerorContext);
        }
    }

    private sealed class OuterTestQueryMiddleware<TQuery, TResponse>(MiddlewareFn middlewareFn) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
                return (response as TestQueryResponse)!;
            });
        }
    }

    private sealed class TestQueryMiddleware<TQuery, TResponse>(MiddlewareFn middlewareFn) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
                return (response as TestQueryResponse)!;
            });
        }
    }

    private sealed class NestedClass(Action<ConquerorContext?> nestedClassFn, IConquerorContextAccessor conquerorContextAccessor)
    {
        public void Execute()
        {
            nestedClassFn(conquerorContextAccessor.ConquerorContext);
        }
    }
}
