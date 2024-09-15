using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamingRequestIdTests
{
    [Test]
    public async Task GivenRequestExecution_RequestIdIsTheSameInProducerAndMiddlewareAndNestedClass()
    {
        var request = new TestStreamingRequest(10);
        var observedRequestIds = new List<string?>();

        var provider = Setup((req, ctx) =>
                             {
                                 observedRequestIds.Add(ctx!.GetStreamingRequestId());
                                 return AsyncEnumerableHelper.Of(new TestItem(req.Payload));
                             },
                             (req, _) => AsyncEnumerableHelper.Of(new NestedTestItem(req.Payload)),
                             (ctx, next) =>
                             {
                                 observedRequestIds.Add(ctx.ConquerorContext.GetStreamingRequestId());
                                 return next(ctx.Request);
                             },
                             (ctx, next) => next(ctx.Request),
                             ctx => { observedRequestIds.Add(ctx!.GetStreamingRequestId()); });

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedRequestIds, Has.Count.EqualTo(3));
        Assert.That(observedRequestIds[1], Is.SameAs(observedRequestIds[0]));
        Assert.That(observedRequestIds[2], Is.SameAs(observedRequestIds[0]));
    }

    [Test]
    public async Task GivenRequestExecution_RequestIdIsNotTheSameInNestedProducer()
    {
        var request = new TestStreamingRequest(10);
        var observedRequestIds = new List<string?>();

        var provider = Setup((req, ctx) =>
                             {
                                 observedRequestIds.Add(ctx!.GetStreamingRequestId());
                                 return AsyncEnumerableHelper.Of(new TestItem(req.Payload));
                             },
                             (req, ctx) =>
                             {
                                 observedRequestIds.Add(ctx!.GetStreamingRequestId());
                                 return AsyncEnumerableHelper.Of(new NestedTestItem(req.Payload));
                             });

        _ = await provider.GetRequiredService<IStreamProducer<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedRequestIds, Has.Count.EqualTo(2));
        Assert.That(observedRequestIds[1], Is.Not.SameAs(observedRequestIds[0]));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestStreamingRequest, ConquerorContext?, IAsyncEnumerable<TestItem>>? producerFn = null,
                                   Func<NestedTestStreamingRequest, ConquerorContext?, IAsyncEnumerable<NestedTestItem>>? nestedProducerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<ConquerorContext?>? nestedClassFn = null)
    {
        producerFn ??= (request, _) => AsyncEnumerableHelper.Of(new TestItem(request.Payload));
        nestedProducerFn ??= (request, _) => AsyncEnumerableHelper.Of(new NestedTestItem(request.Payload));
        middlewareFn ??= (ctx, next) => next(ctx.Request);
        outerMiddlewareFn ??= (ctx, next) => next(ctx.Request);
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorStreamProducer<TestStreamProducer>(p => new(producerFn,
                                                                             p.GetRequiredService<IConquerorContextAccessor>(),
                                                                             p.GetRequiredService<NestedClass>(),
                                                                             p.GetRequiredService<IStreamProducer<NestedTestStreamingRequest, NestedTestItem>>()));

        _ = services.AddConquerorStreamProducer<NestedTestStreamProducer>(p => new(nestedProducerFn, p.GetRequiredService<IConquerorContextAccessor>()));

        _ = services.AddConquerorStreamProducerMiddleware<TestStreamProducerMiddleware>(_ => new(middlewareFn));

        _ = services.AddConquerorStreamProducerMiddleware<OuterTestStreamProducerMiddleware>(_ => new(outerMiddlewareFn));

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestStreamProducer>();
        _ = provider.GetRequiredService<TestStreamProducerMiddleware>();
        _ = provider.GetRequiredService<OuterTestStreamProducerMiddleware>();

        return provider.CreateScope().ServiceProvider;
    }

    private delegate IAsyncEnumerable<TestItem> MiddlewareFn(StreamProducerMiddlewareContext<TestStreamingRequest, TestItem> middlewareCtx,
                                                             Func<TestStreamingRequest, IAsyncEnumerable<TestItem>> next);

    private sealed record TestStreamingRequest(int Payload);

    private sealed record TestItem(int Payload);

    private sealed record NestedTestStreamingRequest(int Payload);

    private sealed record NestedTestItem(int Payload);

    private sealed class TestStreamProducer(
        Func<TestStreamingRequest, ConquerorContext?, IAsyncEnumerable<TestItem>> producerFn,
        IConquerorContextAccessor conquerorContextAccessor,
        NestedClass nestedClass,
        IStreamProducer<NestedTestStreamingRequest, NestedTestItem> nestedStreamProducer)
        : IStreamProducer<TestStreamingRequest, TestItem>, IConfigureStreamProducerPipeline
    {
        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = producerFn(request, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedStreamProducer.ExecuteRequest(new(request.Payload), cancellationToken).Drain();

            await foreach (var item in response)
            {
                yield return item;
            }
        }

        public static void ConfigurePipeline(IStreamProducerPipelineBuilder pipeline) => pipeline.Use<OuterTestStreamProducerMiddleware>()
                                                                                                 .Use<TestStreamProducerMiddleware>();
    }

    private sealed class NestedTestStreamProducer(Func<NestedTestStreamingRequest, ConquerorContext?, IAsyncEnumerable<NestedTestItem>> producerFn, IConquerorContextAccessor conquerorContextAccessor)
        : IStreamProducer<NestedTestStreamingRequest, NestedTestItem>
    {
        public async IAsyncEnumerable<NestedTestItem> ExecuteRequest(NestedTestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await foreach (var item in producerFn(request, conquerorContextAccessor.ConquerorContext))
            {
                yield return item;
            }
        }
    }

    private sealed class OuterTestStreamProducerMiddleware(MiddlewareFn middlewareFn) : IStreamProducerMiddleware
    {
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

    private sealed class TestStreamProducerMiddleware(MiddlewareFn middlewareFn) : IStreamProducerMiddleware
    {
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

    private sealed class NestedClass(Action<ConquerorContext?> nestedClassFn, IConquerorContextAccessor conquerorContextAccessor)
    {
        public void Execute()
        {
            nestedClassFn(conquerorContextAccessor.ConquerorContext);
        }
    }
}
