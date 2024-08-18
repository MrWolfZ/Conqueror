using System.Runtime.CompilerServices;

namespace Conqueror.Streaming.Tests;

public sealed class StreamingRequestIdTests
{
    [Test]
    public async Task GivenRequestExecution_RequestIdIsTheSameInHandlerAndMiddlewareAndNestedClass()
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

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedRequestIds, Has.Count.EqualTo(3));
        Assert.That(observedRequestIds[1], Is.SameAs(observedRequestIds[0]));
        Assert.That(observedRequestIds[2], Is.SameAs(observedRequestIds[0]));
    }

    [Test]
    public async Task GivenRequestExecution_RequestIdIsNotTheSameInNestedHandler()
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

        _ = await provider.GetRequiredService<IStreamingRequestHandler<TestStreamingRequest, TestItem>>()
                          .ExecuteRequest(request, CancellationToken.None)
                          .Drain();

        Assert.That(observedRequestIds, Has.Count.EqualTo(2));
        Assert.That(observedRequestIds[1], Is.Not.SameAs(observedRequestIds[0]));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>>? handlerFn = null,
                                   Func<NestedTestStreamingRequest, IConquerorContext?, IAsyncEnumerable<NestedTestItem>>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<IConquerorContext?>? nestedClassFn = null)
    {
        handlerFn ??= (request, _) => AsyncEnumerableHelper.Of(new TestItem(request.Payload));
        nestedHandlerFn ??= (request, _) => AsyncEnumerableHelper.Of(new NestedTestItem(request.Payload));
        middlewareFn ??= (ctx, next) => next(ctx.Request);
        outerMiddlewareFn ??= (ctx, next) => next(ctx.Request);
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorStreamingRequestHandler<TestStreamingRequestHandler>(p => new(handlerFn,
                                                                                               p.GetRequiredService<IConquerorContextAccessor>(),
                                                                                               p.GetRequiredService<NestedClass>(),
                                                                                               p.GetRequiredService<IStreamingRequestHandler<NestedTestStreamingRequest, NestedTestItem>>()));

        _ = services.AddConquerorStreamingRequestHandler<NestedTestStreamingRequestHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()));

        _ = services.AddConquerorStreamingRequestMiddleware<TestStreamingRequestMiddleware>(_ => new(middlewareFn));

        _ = services.AddConquerorStreamingRequestMiddleware<OuterTestStreamingRequestMiddleware>(_ => new(outerMiddlewareFn));

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestStreamingRequestHandler>();
        _ = provider.GetRequiredService<TestStreamingRequestMiddleware>();
        _ = provider.GetRequiredService<OuterTestStreamingRequestMiddleware>();

        return provider.CreateScope().ServiceProvider;
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

        public TestStreamingRequestHandler(Func<TestStreamingRequest, IConquerorContext?, IAsyncEnumerable<TestItem>> handlerFn,
                                           IConquerorContextAccessor conquerorContextAccessor,
                                           NestedClass nestedClass,
                                           IStreamingRequestHandler<NestedTestStreamingRequest, NestedTestItem> nestedStreamingRequestHandler)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.nestedClass = nestedClass;
            this.nestedStreamingRequestHandler = nestedStreamingRequestHandler;
        }

        public async IAsyncEnumerable<TestItem> ExecuteRequest(TestStreamingRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(request, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedStreamingRequestHandler.ExecuteRequest(new(request.Payload), cancellationToken).Drain();

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
