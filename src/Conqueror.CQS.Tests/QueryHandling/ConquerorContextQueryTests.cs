// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;

namespace Conqueror.CQS.Tests.QueryHandling;

public sealed class ConquerorContextQueryTests
{
    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInHandler()
    {
        var query = new TestQuery(10);

        var provider = Setup((q, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return new(q.Payload);
        });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInMiddleware()
    {
        var query = new TestQuery(10);
        var response = new TestQueryResponse(11);

        var provider = Setup((_, _) => response, middlewareFn: async (ctx, next) =>
        {
            Assert.That(ctx.ConquerorContext, Is.Not.Null);

            return await next(ctx.Query);
        });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInNestedClass()
    {
        var query = new TestQuery(10);

        var provider = Setup(
            nestedClassFn: b => Assert.That(b, Is.Not.Null),
            nestedClassLifetime: ServiceLifetime.Scoped);

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInNestedHandler()
    {
        var query = new TestQuery(10);

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return new(q.Payload);
        });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInHandlerAfterExecutionOfNestedHandler()
    {
        var query = new TestQuery(10);

        var provider = Setup(handlerPreReturnFn: b => Assert.That(b, Is.Not.Null));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    [Combinatorial]
    public async Task GivenQueryExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassRegardlessOfLifetime(
        [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime handlerLifetime,
        [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime middlewareLifetime,
        [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime nestedClassLifetime)
    {
        var query = new TestQuery(10);
        var observedContexts = new List<IConquerorContext>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedContexts.Add(ctx!);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            (ctx, next) =>
            {
                observedContexts.Add(ctx.ConquerorContext);
                return next(ctx.Query);
            },
            (ctx, next) => next(ctx.Query),
            ctx => observedContexts.Add(ctx!),
            _ => { },
            handlerLifetime,
            middlewareLifetime,
            nestedClassLifetime);

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassWithConfigureAwait()
    {
        var query = new TestQuery(10);
        var observedContexts = new List<IConquerorContext>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedContexts.Add(ctx!);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            async (ctx, next) =>
            {
                await Task.Delay(10).ConfigureAwait(false);
                observedContexts.Add(ctx.ConquerorContext);
                return await next(ctx.Query);
            },
            (ctx, next) => next(ctx.Query),
            ctx => observedContexts.Add(ctx!));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenQueryExecution_TraceIdIsTheSameInHandlerMiddlewareAndNestedClass()
    {
        var query = new TestQuery(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            (ctx, next) =>
            {
                observedTraceIds.Add(ctx.ConquerorContext.TraceId);
                return next(ctx.Query);
            },
            (ctx, next) => next(ctx.Query),
            ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInHandlerMiddlewareAndNestedClass()
    {
        using var activity = StartActivity(nameof(GivenQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInHandlerMiddlewareAndNestedClass));

        var query = new TestQuery(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            (ctx, next) =>
            {
                observedTraceIds.Add(ctx.ConquerorContext.TraceId);
                return next(ctx.Query);
            },
            (ctx, next) => next(ctx.Query),
            ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public async Task GivenQueryExecution_TraceIdIsTheSameInNestedHandler()
    {
        var query = new TestQuery(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(q.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedHandler));

        var query = new TestQuery(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(q.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public void GivenNoQueryExecution_ConquerorContextIsNotAvailable()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>();

        _ = services.AddTransient(p => new NestedClass(b => Assert.That(b, Is.Null), p.GetRequiredService<IConquerorContextAccessor>()));

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<NestedClass>().Execute();
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInHandler()
    {
        var query = new TestQuery(10);
        var expectedTraceId = string.Empty;

        var provider = Setup((q, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler));

        var query = new TestQuery(10);

        var provider = Setup((q, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInNestedHandler()
    {
        var query = new TestQuery(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler));

        var query = new TestQuery(10);

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestQuery, IConquerorContext?, TestQueryResponse>? handlerFn = null,
                                   Func<NestedTestQuery, IConquerorContext?, NestedTestQueryResponse>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<IConquerorContext?>? nestedClassFn = null,
                                   Action<IConquerorContext?>? handlerPreReturnFn = null,
                                   ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedHandlerLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
    {
        handlerFn ??= (query, _) => new(query.Payload);
        nestedHandlerFn ??= (query, _) => new(query.Payload);
        middlewareFn ??= (middlewareCtx, next) => next(middlewareCtx.Query);
        outerMiddlewareFn ??= (middlewareCtx, next) => next(middlewareCtx.Query);
        nestedClassFn ??= _ => { };
        handlerPreReturnFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedClassLifetime));

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(p => new(handlerFn,
                                                                         handlerPreReturnFn,
                                                                         p.GetRequiredService<IConquerorContextAccessor>(),
                                                                         p.GetRequiredService<NestedClass>(),
                                                                         p.GetRequiredService<IQueryHandler<NestedTestQuery, NestedTestQueryResponse>>()),
                                                                handlerLifetime);

        _ = services.AddConquerorQueryHandler<NestedTestQueryHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                                      nestedHandlerLifetime);

        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(TestQueryMiddleware), middlewareFn);
        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(OuterTestQueryMiddleware), outerMiddlewareFn);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestQueryHandler>();

        return provider.CreateScope().ServiceProvider;
    }

    private static DisposableActivity StartActivity(string name)
    {
        var activitySource = new ActivitySource(name);

        var activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };

        ActivitySource.AddActivityListener(activityListener);

        var activity = activitySource.StartActivity()!;
        return new(activity.TraceId.ToString(), activitySource, activityListener, activity);
    }

    private sealed class DisposableActivity : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables;

        public DisposableActivity(string traceId, params IDisposable[] disposables)
        {
            TraceId = traceId;
            this.disposables = disposables;
        }

        public string TraceId { get; }

        public void Dispose()
        {
            foreach (var disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }

    private delegate Task<TestQueryResponse> MiddlewareFn(QueryMiddlewareContext<TestQuery, TestQueryResponse> middlewareCtx,
                                                          Func<TestQuery, Task<TestQueryResponse>> next);

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int Payload);

    private sealed record NestedTestQuery(int Payload);

    private sealed record NestedTestQueryResponse(int Payload);

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Func<TestQuery, IConquerorContext?, TestQueryResponse> handlerFn;
        private readonly NestedClass nestedClass;
        private readonly IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler;
        private readonly Action<IConquerorContext?> preReturnFn;

        public TestQueryHandler(Func<TestQuery, IConquerorContext?, TestQueryResponse> handlerFn,
                                Action<IConquerorContext?> preReturnFn,
                                IConquerorContextAccessor conquerorContextAccessor,
                                NestedClass nestedClass,
                                IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.nestedClass = nestedClass;
            this.nestedQueryHandler = nestedQueryHandler;
            this.preReturnFn = preReturnFn;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(query, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedQueryHandler.ExecuteQuery(new(query.Payload), cancellationToken);
            preReturnFn(conquerorContextAccessor.ConquerorContext);
            return response;
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline) => pipeline.Use(new OuterTestQueryMiddleware(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFn>(typeof(OuterTestQueryMiddleware))))
                                                                                                               .Use(new TestQueryMiddleware(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFn>(typeof(TestQueryMiddleware))));
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
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, async query =>
            {
                var response = await ctx.Next((query as TQuery)!, ctx.CancellationToken);
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
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, async query =>
            {
                var response = await ctx.Next((query as TQuery)!, ctx.CancellationToken);
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
