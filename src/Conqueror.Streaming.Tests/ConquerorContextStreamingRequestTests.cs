// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class ConquerorContextStreamingRequestTests
{
    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInProducer()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup((q, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
        });

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
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

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInNestedClass()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup(nestedClassFn: b => Assert.That(b, Is.Not.Null),
                             nestedClassLifetime: ServiceLifetime.Scoped);

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInNestedProducer()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup(nestedProducerFn: (q, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
        });

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsAvailableInProducerAfterExecutionOfNestedProducer()
    {
        var request = new TestStreamingRequest(10);

        var provider = Setup(producerPreReturnFn: b => Assert.That(b, Is.Not.Null));

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    [Combinatorial]
    public async Task GivenRequestExecution_ConquerorContextIsTheSameInMiddlewareProducerAndNestedClassRegardlessOfLifetime([Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)] ServiceLifetime producerLifetime,
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
                             producerLifetime,
                             middlewareLifetime,
                             nestedClassLifetime);

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenRequestExecution_ConquerorContextIsTheSameInMiddlewareProducerAndNestedClassWithConfigureAwait()
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

        async IAsyncEnumerable<TestItem> NestedMiddlewareFn(StreamProducerMiddlewareContext<TestStreamingRequest, TestItem> ctx,
                                                            Func<TestStreamingRequest, IAsyncEnumerable<TestItem>> next)
        {
            await Task.Delay(10).ConfigureAwait(false);
            observedContexts.Add(ctx.ConquerorContext);

            await foreach (var item in next(ctx.Request))
            {
                yield return item;
            }
        }

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenRequestExecution_TraceIdIsTheSameInProducerMiddlewareAndNestedClass()
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

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInProducerMiddlewareAndNestedClass()
    {
        using var activity = StartActivity(nameof(GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInProducerMiddlewareAndNestedClass));

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

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public async Task GivenRequestExecution_TraceIdIsTheSameInNestedProducer()
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

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedProducer()
    {
        using var activity = StartActivity(nameof(GivenRequestExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedProducer));

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

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public void GivenNoRequestExecution_ConquerorContextIsNotAvailable()
    {
        var services = new ServiceCollection().AddConquerorStreamProducer<TestStreamProducer>();

        _ = services.AddTransient(p => new NestedClass(b => Assert.That(b, Is.Null), p.GetRequiredService<IConquerorContextAccessor>()));

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<NestedClass>().Execute();
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInProducer()
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

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInProducer()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInProducer));

        var request = new TestStreamingRequest(10);

        var provider = Setup((q, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return AsyncEnumerableHelper.Of(new TestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInNestedProducer()
    {
        var request = new TestStreamingRequest(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedProducerFn: (q, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedProducer()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedProducer));

        var request = new TestStreamingRequest(10);

        var provider = Setup(nestedProducerFn: (q, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return AsyncEnumerableHelper.Of(new NestedTestItem(q.Payload));
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>>? producerFn = null,
                                   Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>>? nestedProducerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<IConquerorContext?>? nestedClassFn = null,
                                   Action<IConquerorContext?>? producerPreReturnFn = null,
                                   ServiceLifetime producerLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedProducerLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
    {
        producerFn ??= (request, _) => AsyncEnumerableHelper.Of(new TestItem(request.Payload));
        nestedProducerFn ??= (request, _) => AsyncEnumerableHelper.Of(new NestedTestItem(request.Payload));
        middlewareFn ??= (middlewareCtx, next) => next(middlewareCtx.Request);
        outerMiddlewareFn ??= (middlewareCtx, next) => next(middlewareCtx.Request);
        nestedClassFn ??= _ => { };
        producerPreReturnFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedClassLifetime));

        _ = services.AddConquerorStreamProducer<TestStreamProducer>(p => new(producerFn,
                                                                             producerPreReturnFn,
                                                                             p.GetRequiredService<IConquerorContextAccessor>(),
                                                                             p.GetRequiredService<NestedClass>(),
                                                                             p.GetRequiredService<IStreamProducer<NestedTestStreamingRequest, NestedTestItem>>()),
                                                                    producerLifetime);

        _ = services.AddConquerorStreamProducer<NestedTestStreamProducer>(p => new(nestedProducerFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                                          nestedProducerLifetime);

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(_ => new(middlewareFn), middlewareLifetime);

        _ = services.AddConquerorStreamProducerMiddleware<OuterTestStreamProducerMiddleware>(_ => new(outerMiddlewareFn), middlewareLifetime);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestStreamProducer>();
        _ = provider.GetRequiredService<TestStreamProducerMiddleware>();
        _ = provider.GetRequiredService<OuterTestStreamProducerMiddleware>();

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

    private delegate IAsyncEnumerable<TestItem> MiddlewareFn(StreamProducerMiddlewareContext<TestStreamingRequest, TestItem> middlewareCtx,
                                                             Func<TestStreamingRequest, IAsyncEnumerable<TestItem>> next);

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed record NestedTestStreamingRequest(int Payload);

    private sealed record NestedTestItem(int Payload);

    private sealed class TestStreamProducer : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly NestedClass nestedClass;
        private readonly IStreamProducer<NestedTestStreamingRequest, NestedTestItem> nestedStreamProducer;
        private readonly Action<IConquerorContext?> preReturnFn;
        private readonly Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>> producerFn;

        public TestStreamProducer(Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>> producerFn,
                                  Action<IConquerorContext?> preReturnFn,
                                  IConquerorContextAccessor conquerorContextAccessor,
                                  NestedClass nestedClass,
                                  IStreamProducer<NestedTestStreamingRequest, NestedTestItem> nestedStreamProducer)
        {
            this.producerFn = producerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.nestedClass = nestedClass;
            this.nestedStreamProducer = nestedStreamProducer;
            this.preReturnFn = preReturnFn;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = producerFn(request, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedStreamProducer.ExecuteRequest(new(request.Payload), cancellationToken).Drain();
            preReturnFn(conquerorContextAccessor.ConquerorContext);
            await foreach (var item in response)
            {
                yield return item;
            }
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) => pipeline.Use<OuterTestStreamProducerMiddleware>()
                                                                                                 .Use<TestStreamProducerMiddleware>();
    }

    private sealed class NestedTestStreamProducer : IStreamProducer<NestedTestStreamingRequest, NestedTestItem>
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>> producerFn;

        public NestedTestStreamProducer(Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>> producerFn, IConquerorContextAccessor conquerorContextAccessor)
        {
            this.producerFn = producerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
        }

        public async IAsyncEnumerable<NestedTestItem> ExecuteRequest(NestedTestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            await foreach (var item in producerFn(request, conquerorContextAccessor.ConquerorContext))
            {
                yield return item;
            }
        }
    }

    private sealed class OuterTestStreamProducerMiddleware : IStreamProducerMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public OuterTestStreamProducerMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            var castedCtx = (ctx as StreamProducerMiddlewareContext<TestStreamingRequest, TestItem>)!;

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

    private sealed class TestStreamProducerMiddleware : IStreamProducerMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public TestStreamProducerMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async IAsyncEnumerable<TItem> Execute<TRequest, TItem>(StreamProducerMiddlewareContext<TRequest, TItem> ctx)
            where TRequest : class
        {
            await Task.Yield();
            var castedCtx = (ctx as StreamProducerMiddlewareContext<TestStreamingRequest, TestItem>)!;

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
