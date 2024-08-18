// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class ConquerorContextStreamingRequestTests
{
    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInHandler()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup((q, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
        });

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInMiddleware()
    {
        var request = new TestStreamingRequest(10);
        var response = AsyncEnumerableHelper.Of(new TestItem(11));

        var provider = Setup((_, _) => response, middlewareFn: (ctx, next) =>
        {
            Assert.That(ctx.ConquerorContext, Is.Not.Null);

            return next(ctx.Request);
        });

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInNestedClass()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup(nestedClassFn: b => Assert.That(b, Is.Not.Null),
                             nestedClassLifetime: ServiceLifetime.Scoped);

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInNestedHandler()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
        });

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInHandlerAfterExecutionOfNestedHandler()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup(handlerPreReturnFn: b => Assert.That(b, Is.Not.Null));

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    [Combinatorial]
    public async Task GivenRequestExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassRegardlessOfLifetime([Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)] ServiceLifetime handlerLifetime,
                                                                                                                           [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
                                                                                                                           ServiceLifetime middlewareLifetime,
                                                                                                                           [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
                                                                                                                           ServiceLifetime nestedClassLifetime)
    {
        var request = new TestStreamingRequest(10);
        var observedContexts = new List<IConquerorContext>();

        var provider = Setup((q, ctx) =>
                             {
                                 observedContexts.Add(ctx!);
                                 return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
                             },
                             (q, _) => AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload)),
                             (ctx, next) =>
                             {
                                 observedContexts.Add(ctx.ConquerorContext);
                                 return next(ctx.Request);
                             },
                             (ctx, next) => next(ctx.Request),
                             ctx => observedContexts.Add(ctx!),
                             _ => { },
                             handlerLifetime,
                             middlewareLifetime,
                             nestedClassLifetime);

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassWithConfigureAwait()
    {
        var request = new TestStreamingRequest(10);
        var observedContexts = new List<IConquerorContext>();

        var provider = Setup((q, ctx) =>
                             {
                                 observedContexts.Add(ctx!);
                                 return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
                             },
                             (q, _) => AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload)),
                             NestedMiddlewareFn,
                             (ctx, next) => next(ctx.Request),
                             ctx => observedContexts.Add(ctx!));

        async IAsyncEnumerable<TestItem> NestedMiddlewareFn(StreamingRequestMiddlewareContext<TestStreamingRequest, TestItem> ctx,
                                                            Func<TestStreamingRequest, IAsyncEnumerable<TestItem>> next)
        {
            await Task.Delay(10).ConfigureAwait(false);
            observedContexts.Add(ctx.ConquerorContext);

            await foreach (var item in next(ctx.Request))
            {
                yield return item;
            }
        }

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenRequestExecution_TraceIdIsTheSameInHandlerMiddlewareAndNestedClass()
    {
        var request = new TestStreamingRequest(10);
        var observedTraceIds = new List<string>();

        var provider = Setup((q, ctx) =>
                             {
                                 observedTraceIds.Add(ctx!.TraceId);
                                 return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
                             },
                             (q, _) => AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload)),
                             (ctx, next) =>
                             {
                                 observedTraceIds.Add(ctx.ConquerorContext.TraceId);
                                 return next(ctx.Request);
                             },
                             (ctx, next) => next(ctx.Request),
                             ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInHandlerMiddlewareAndNestedClass()
    {
        using var activity = StartActivity(nameof(GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInHandlerMiddlewareAndNestedClass));

        var request = new TestStreamingRequest(10);
        var observedTraceIds = new List<string>();

        var provider = Setup((q, ctx) =>
                             {
                                 observedTraceIds.Add(ctx!.TraceId);
                                 return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
                             },
                             (q, _) => AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload)),
                             (ctx, next) =>
                             {
                                 observedTraceIds.Add(ctx.ConquerorContext.TraceId);
                                 return next(ctx.Request);
                             },
                             (ctx, next) => next(ctx.Request),
                             ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public async Task GivenRequestExecution_TraceIdIsTheSameInNestedHandler()
    {
        var request = new TestStreamingRequest(10);
        var observedTraceIds = new List<string>();

        var provider = Setup((q, ctx) =>
                             {
                                 observedTraceIds.Add(ctx!.TraceId);
                                 return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
                             },
                             (q, ctx) =>
                             {
                                 observedTraceIds.Add(ctx!.TraceId);
                                 return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
                             });

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedHandler));

        var request = new TestStreamingRequest(10);
        var observedTraceIds = new List<string>();

        var provider = Setup((q, ctx) =>
                             {
                                 observedTraceIds.Add(ctx!.TraceId);
                                 return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
                             },
                             (q, ctx) =>
                             {
                                 observedTraceIds.Add(ctx!.TraceId);
                                 return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
                             });

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public void GivenNoRequestExecution_ConquerorContextIsNotAvailable()
    {
        var services = new ServiceCollection().AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>();

        _ = services.AddTransient(p => new NestedClass(b => Assert.That(b, Is.Null), p.GetRequiredService<IConquerorContextAccessor>()));

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<NestedClass>().Execute();
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInHandler()
    {
        var request = new TestStreamingRequest(10);
        var expectedTraceId = string.Empty;

        var provider = Setup((q, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler));

        var request = new TestStreamingRequest(10);

        var provider = Setup((q, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInNestedHandler()
    {
        var request = new TestStreamingRequest(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler));

        var request = new TestStreamingRequest(10);

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>>? handlerFn = null,
                                   Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<IConquerorContext?>? nestedClassFn = null,
                                   Action<IConquerorContext?>? handlerPreReturnFn = null,
                                   ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedHandlerLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
    {
        handlerFn ??= (request, _) => AsyncEnumerableHelper.Of(new TestItem(request.Payload));
        nestedHandlerFn ??= (request, _) => AsyncEnumerableHelper.Of(new NestedTestItem(request.Payload));
        middlewareFn ??= (middlewareCtx, next) => next(middlewareCtx.Request);
        outerMiddlewareFn ??= (middlewareCtx, next) => next(middlewareCtx.Request);
        nestedClassFn ??= _ => { };
        handlerPreReturnFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedClassLifetime));

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(p => new(handlerFn,
                                                                                               handlerPreReturnFn,
                                                                                               p.GetRequiredService<IConquerorContextAccessor>(),
                                                                                               p.GetRequiredService<NestedClass>(),
                                                                                               p.GetRequiredService<IStreamingRequestHandler<NestedTestStreamingRequest, NestedTestItem>>()),
                                                                                      handlerLifetime);

        _ = services.AddConquerorStreamingRequestHandler<NestedTestStreamingRequestHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                                                            nestedHandlerLifetime);

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(_ => new(middlewareFn), middlewareLifetime);

        _ = services.AddConquerorStreamingRequestMiddleware<OuterTestStreamingRequestMiddleware>(_ => new(outerMiddlewareFn), middlewareLifetime);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestStreamingRequestHandler>();
        _ = provider.GetRequiredService<TestStreamingRequestMiddleware>();
        _ = provider.GetRequiredService<OuterTestStreamingRequestMiddleware>();

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

    private delegate IAsyncEnumerable<TestItem> MiddlewareFn(StreamingRequestMiddlewareContext<TestStreamingRequest, TestItem> middlewareCtx,
                                                             Func<TestStreamingRequest, IAsyncEnumerable<TestItem>> next);

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed record NestedTestStreamingRequest(int Payload);

    private sealed record NestedTestItem(int Payload);

    private sealed class TestStreamingRequestHandler : IStreamingRequestHandler<TestStreamingRequest, TestItem>, IConfigureStreamingRequestPipeline
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>> handlerFn;
        private readonly NestedClass nestedClass;
        private readonly IStreamingRequestHandler<NestedTestStreamingRequest, NestedTestItem> nestedStreamingRequestHandler;
        private readonly Action<IConquerorContext?> preReturnFn;

        public TestStreamingRequestHandler(Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>> handlerFn,
                                           Action<IConquerorContext?> preReturnFn,
                                           IConquerorContextAccessor conquerorContextAccessor,
                                           NestedClass nestedClass,
                                           IStreamingRequestHandler<NestedTestStreamingRequest, NestedTestItem> nestedStreamingRequestHandler)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.nestedClass = nestedClass;
            this.nestedStreamingRequestHandler = nestedStreamingRequestHandler;
            this.preReturnFn = preReturnFn;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(request, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedStreamingRequestHandler.ExecuteRequest(new(request.Payload), cancellationToken).Drain();
            preReturnFn(conquerorContextAccessor.ConquerorContext);
            await foreach (var item in response)
            {
                yield return item;
            }
        }

        public static void ConfigurePipeline(IStreamingRequestPipelineBuilder pipeline) => pipeline.Use<OuterTestStreamingRequestMiddleware>()
                                                                                                   .Use<TestStreamingRequestMiddleware>();
    }

    private sealed class NestedTestStreamingRequestHandler : IStreamingRequestHandler<NestedTestStreamingRequest, NestedTestItem>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>> handlerFn;

        public NestedTestStreamingRequestHandler(Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>> handlerFn, IConquerorContextAccessor conquerorContextAccessor)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
        }

        public async IAsyncEnumerable<NestedTestItem> ExecuteRequest(NestedTestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            await foreach (var item in handlerFn(request, conquerorContextAccessor.ConquerorContext))
            {
                yield return item;
            }
        }
    }

    private sealed class OuterTestStreamingRequestMiddleware : IStreamingRequestMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public OuterTestStreamingRequestMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            var castedCtx = (ctx as StreamingRequestMiddlewareContext<TestStreamingRequest, TestItem>)!;

            await foreach (var item in middlewareFn(castedCtx, MiddlewareFn))
            {
                yield return (TItem)(object)item;
            }

            yield break;

            async IAsyncEnumerable<TestItem> MiddlewareFn(TestStreamingRequest request)
            {
                await foreach (var i in ctx.Next((request as TRequest)!, ctx.CancellationToken))
                {
                    yield return (i as TestItem)!;
                }
            }
        }
    }

    private sealed class TestStreamingRequestMiddleware : IStreamingRequestMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public TestStreamingRequestMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamingRequestMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            var castedCtx = (ctx as StreamingRequestMiddlewareContext<TestStreamingRequest, TestItem>)!;

            await foreach (var item in middlewareFn(castedCtx, MiddlewareFn))
            {
                yield return (TItem)(object)item;
            }

            yield break;

            async IAsyncEnumerable<TestItem> MiddlewareFn(TestStreamingRequest request)
            {
                await foreach (var i in ctx.Next((request as TRequest)!, ctx.CancellationToken))
                {
                    yield return (i as TestItem)!;
                }
            }
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
