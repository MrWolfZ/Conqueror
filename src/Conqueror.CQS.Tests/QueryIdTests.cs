namespace Conqueror.CQS.Tests;

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
    private IServiceProvider Setup(Func<TestQuery, IConquerorContext?, TestQueryResponse>? handlerFn = null,
                                   Func<NestedTestQuery, IConquerorContext?, NestedTestQueryResponse>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<IConquerorContext?>? nestedClassFn = null)
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

        _ = services.AddConquerorQueryMiddleware<TestQueryMiddleware>(_ => new(middlewareFn));

        _ = services.AddConquerorQueryMiddleware<OuterTestQueryMiddleware>(_ => new(outerMiddlewareFn));

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestQueryHandler>();
        _ = provider.GetRequiredService<TestQueryMiddleware>();
        _ = provider.GetRequiredService<OuterTestQueryMiddleware>();

        return provider.CreateScope().ServiceProvider;
    }

    private delegate Task<TestQueryResponse> MiddlewareFn(QueryMiddlewareContext<TestQuery, TestQueryResponse> middlewareCtx,
                                                          Func<TestQuery, Task<TestQueryResponse>> next);

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int Payload);

    private sealed record NestedTestQuery(int Payload);

    private sealed record NestedTestQueryResponse(int Payload);

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Func<TestQuery, IConquerorContext?, TestQueryResponse> handlerFn;
        private readonly NestedClass nestedClass;
        private readonly IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler;

        public TestQueryHandler(Func<TestQuery, IConquerorContext?, TestQueryResponse> handlerFn,
                                IConquerorContextAccessor conquerorContextAccessor,
                                NestedClass nestedClass,
                                IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.nestedClass = nestedClass;
            this.nestedQueryHandler = nestedQueryHandler;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(query, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedQueryHandler.ExecuteQuery(new(query.Payload), cancellationToken);
            return response;
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.Use<OuterTestQueryMiddleware>()
                                                                                        .Use<TestQueryMiddleware>();
    }

    private sealed class NestedTestQueryHandler : IQueryHandler<NestedTestQuery, NestedTestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Func<NestedTestQuery, IConquerorContext?, NestedTestQueryResponse> handlerFn;

        public NestedTestQueryHandler(Func<NestedTestQuery, IConquerorContext?, NestedTestQueryResponse> handlerFn, IConquerorContextAccessor conquerorContextAccessor)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
        }

        public async Task<NestedTestQueryResponse> ExecuteQuery(NestedTestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return handlerFn(query, conquerorContextAccessor.ConquerorContext);
        }
    }

    private sealed class OuterTestQueryMiddleware : IQueryMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public OuterTestQueryMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
                return (response as TestQueryResponse)!;
            });
        }
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public TestQueryMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
                return (response as TestQueryResponse)!;
            });
        }
    }

    private sealed class NestedClass
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Action<IConquerorContext?> nestedClassFn;

        public NestedClass(Action<IConquerorContext?> nestedClassFn, IConquerorContextAccessor conquerorContextAccessor)
        {
            this.nestedClassFn = nestedClassFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
        }

        public void Execute()
        {
            nestedClassFn(conquerorContextAccessor.ConquerorContext);
        }
    }
}
