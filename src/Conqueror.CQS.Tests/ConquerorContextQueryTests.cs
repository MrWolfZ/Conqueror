// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;

namespace Conqueror.CQS.Tests;

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

        var provider = Setup((_, _) => response, middlewareFn: async (middlewareCtx, ctx, next) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return await next(middlewareCtx.Query);
        });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInNestedClass()
    {
        var query = new TestQuery(10);

        var provider = Setup(
            nestedClassFn: Assert.IsNotNull,
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

        var provider = Setup(handlerPreReturnFn: Assert.IsNotNull);

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
            (middlewareCtx, ctx, next) =>
            {
                observedContexts.Add(ctx!);
                return next(middlewareCtx.Query);
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
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
    [Combinatorial]
    public async Task GivenQueryExecution_ConquerorContextIsTheSameInNestedHandlerRegardlessOfLifetime(
        [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime handlerLifetime,
        [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime nestedHandlerLifetime)
    {
        var query = new TestQuery(10);
        var observedContexts = new List<IConquerorContext>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedContexts.Add(ctx!);
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                observedContexts.Add(ctx!);
                return new(q.Payload);
            },
            handlerLifetime: handlerLifetime,
            nestedHandlerLifetime: nestedHandlerLifetime);

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedContexts, Has.Count.EqualTo(2));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
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
            async (middlewareCtx, ctx, next) =>
            {
                await Task.Delay(10).ConfigureAwait(false);
                observedContexts.Add(ctx!);
                return await next(middlewareCtx.Query);
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
            ctx => observedContexts.Add(ctx!));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsAreTheSameInHandlerMiddlewareAndNestedClass()
    {
        var query = new TestQuery(10);
        var observedItems = new List<IDictionary<string, string>>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedItems.Add(ctx!.Items);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            (middlewareCtx, ctx, next) =>
            {
                observedItems.Add(ctx!.Items);
                return next(middlewareCtx.Query);
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
            ctx => observedItems.Add(ctx!.Items));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedItems, Has.Count.EqualTo(3));
        Assert.That(observedItems[1], Is.SameAs(observedItems[0]));
        Assert.That(observedItems[2], Is.SameAs(observedItems[0]));
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
            (middlewareCtx, ctx, next) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return next(middlewareCtx.Query);
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
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
            (middlewareCtx, ctx, next) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return next(middlewareCtx.Query);
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
            ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsAreTheSameInNestedHandler()
    {
        var query = new TestQuery(10);
        var observedItems = new List<IDictionary<string, string>>();

        var provider = Setup(
            (q, ctx) =>
            {
                observedItems.Add(ctx!.Items);
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                observedItems.Add(ctx!.Items);
                return new(q.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedItems, Has.Count.EqualTo(2));
        Assert.That(observedItems[1], Is.SameAs(observedItems[0]));
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
    public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSource()
    {
        var query = new TestQuery(10);
        var items = new Dictionary<string, string> { { "key", "value" } };
        var observedKeys = new List<string>();

        var provider = Setup(
            (q, ctx) =>
            {
                ctx!.AddOrReplaceItems(items);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            async (middlewareCtx, ctx, next) =>
            {
                observedKeys.AddRange(ctx!.Items.Keys);
                var r = await next(middlewareCtx.Query);
                observedKeys.AddRange(ctx.Items.Keys);
                return r;
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
            ctx => observedKeys.AddRange(ctx!.Items.Keys));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedKeys, Has.Count.EqualTo(2));
        Assert.That(observedKeys[1], Is.SameAs(observedKeys[0]));
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSourceWithDuplicates()
    {
        var query = new TestQuery(10);
        var items = new Dictionary<string, string> { { "key", "value" } };
        var observedKeys = new List<string>();

        var provider = Setup(
            (q, ctx) =>
            {
                ctx!.AddOrReplaceItems(items);
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            async (middlewareCtx, ctx, next) =>
            {
                observedKeys.AddRange(ctx!.Items.Keys);
                ctx.AddOrReplaceItems(items);
                var r = await next(middlewareCtx.Query);
                observedKeys.AddRange(ctx.Items.Keys);
                return r;
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
            ctx => observedKeys.AddRange(ctx!.Items.Keys));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedKeys, Has.Count.EqualTo(2));
        Assert.That(observedKeys[1], Is.SameAs(observedKeys[0]));
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsCanBeSet()
    {
        var query = new TestQuery(10);
        var key = "test";
        var observedItems = new List<string?>();

        var provider = Setup(
            (q, ctx) =>
            {
                ctx!.Items[key] = key;
                return new(q.Payload);
            },
            (q, _) => new(q.Payload),
            async (middlewareCtx, ctx, next) =>
            {
                observedItems.Add(ctx!.Items.TryGetValue(key, out var k) ? k : null);
                var r = await next(middlewareCtx.Query);
                observedItems.Add(ctx.Items.TryGetValue(key, out var k2) ? k2 : null);
                return r;
            },
            (middlewareCtx, _, next) => next(middlewareCtx.Query),
            ctx => observedItems.Add(ctx!.Items.TryGetValue(key, out var k) ? k : null));

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedItems, Has.Count.EqualTo(3));
        Assert.That(observedItems[0], Is.Null);
        Assert.That(observedItems[1], Is.SameAs(key));
        Assert.That(observedItems[2], Is.SameAs(key));
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSourceForNestedHandler()
    {
        var query = new TestQuery(10);
        var key = "key";
        var items = new Dictionary<string, string> { { key, "value" } };

        var provider = Setup(
            (q, ctx) =>
            {
                ctx!.AddOrReplaceItems(items);
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                Assert.That(ctx?.Items.ContainsKey(key), Is.True);
                return new(q.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsCanBeSetForNestedHandler()
    {
        var query = new TestQuery(10);
        var key = "test";

        var provider = Setup(
            (q, ctx) =>
            {
                ctx!.Items[key] = key;
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                Assert.That(ctx?.Items.ContainsKey(key), Is.True);
                return new(q.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSourceFromNestedHandler()
    {
        var query = new TestQuery(10);
        var key = "key";
        var items = new Dictionary<string, string> { { key, "value" } };

        var provider = Setup(
            nestedHandlerFn: (q, ctx) =>
            {
                ctx!.AddOrReplaceItems(items);
                return new(q.Payload);
            },
            middlewareFn: async (middlewareCtx, ctx, next) =>
            {
                Assert.That(ctx?.Items.ContainsKey(key), Is.False);
                var r = await next(middlewareCtx.Query);
                Assert.That(ctx?.Items.ContainsKey(key), Is.True);
                return r;
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_ContextItemsCanBeSetFromNestedHandler()
    {
        var query = new TestQuery(10);
        var key = "test";

        var provider = Setup(
            nestedHandlerFn: (q, ctx) =>
            {
                ctx!.Items[key] = key;
                return new(q.Payload);
            },
            middlewareFn: async (middlewareCtx, ctx, next) =>
            {
                Assert.That(ctx?.Items.ContainsKey(key), Is.False);
                var r = await next(middlewareCtx.Query);
                Assert.That(ctx?.Items.ContainsKey(key), Is.True);
                return r;
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public void GivenNoQueryExecution_ConquerorContextIsNotAvailable()
    {
        var services = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>();

        _ = services.AddTransient(p => new NestedClass(Assert.IsNull, p.GetRequiredService<IConquerorContextAccessor>()));

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<NestedClass>().Execute();
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithItems_ContextItemsAreAvailableInHandler()
    {
        var query = new TestQuery(10);
        var key = "key";
        var value = "value";

        var provider = Setup((q, ctx) =>
        {
            Assert.That(ctx!.Items.ContainsKey(key), Is.True);
            Assert.That(ctx.Items[key], Is.EqualTo(value));
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.Items[key] = value;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
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
    public async Task GivenManuallyCreatedContextWithItems_ContextItemsAreAvailableInNestedHandler()
    {
        var query = new TestQuery(10);
        var key = "key";
        var value = "value";

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            Assert.That(ctx!.Items.ContainsKey(key), Is.True);
            Assert.That(ctx.Items[key], Is.EqualTo(value));
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.Items[key] = value;

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

    [Test]
    public async Task GivenManuallyCreatedContextWithItems_ContextItemsCanBeChangedInHandlerForNestedHandler()
    {
        var query = new TestQuery(10);
        var key = "key";
        var value = "value";
        var newValue = "newValue";

        var provider = Setup(
            (q, ctx) =>
            {
                Assert.That(ctx!.Items[key], Is.EqualTo(value));
                ctx.Items[key] = newValue;
                return new(q.Payload);
            },
            (q, ctx) =>
            {
                Assert.That(ctx!.Items[key], Is.EqualTo(newValue));
                return new(q.Payload);
            });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.Items[key] = value;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContext_ItemsFromHandlerAreAvailableInClientContext()
    {
        var query = new TestQuery(10);
        var key = "key";
        var value = "value";

        var provider = Setup((q, ctx) =>
        {
            ctx!.Items[key] = value;
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        var contextItems = conquerorContext.Items;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(contextItems[key], Is.EqualTo(value));
    }

    [Test]
    public async Task GivenManuallyCreatedContext_ItemsFromNestedHandlerAreAvailableInClientContext()
    {
        var query = new TestQuery(10);
        var key = "key";
        var value = "value";

        var provider = Setup(nestedHandlerFn: (q, ctx) =>
        {
            ctx!.Items[key] = value;
            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        var contextItems = conquerorContext.Items;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(contextItems[key], Is.EqualTo(value));
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithItems_ContextItemsFlowAcrossMultipleHandlerExecutions()
    {
        var query = new TestQuery(10);
        var invocationCount = 0;
        var key1 = "key 1";
        var key2 = "key 2";
        var value1 = "value 1";
        var value2 = "value 2";
        var value3 = "value 3";
        var value4 = "value 4";

        var provider = Setup((q, ctx) =>
        {
            if (invocationCount == 0)
            {
                Assert.That(ctx!.Items[key1], Is.EqualTo(value1));
                Assert.That(ctx.Items.ContainsKey(key2), Is.False);
                ctx.Items[key2] = value2;
            }
            else if (invocationCount == 1)
            {
                Assert.That(ctx!.Items[key1], Is.EqualTo(value1));
                Assert.That(ctx.Items[key2], Is.EqualTo(value2));
                ctx.Items[key1] = value3;
            }
            else if (invocationCount == 2)
            {
                Assert.That(ctx!.Items[key1], Is.EqualTo(value3));
                Assert.That(ctx.Items[key2], Is.EqualTo(value2));
                ctx.Items[key1] = value4;
            }
            else
            {
                Assert.Fail("should not reach this");
            }

            invocationCount += 1;

            return new(q.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        conquerorContext.Items[key1] = value1;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(conquerorContext.Items[key1], Is.SameAs(value4));
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
                                   ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                   ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
    {
        handlerFn ??= (query, _) => new(query.Payload);
        nestedHandlerFn ??= (query, _) => new(query.Payload);
        middlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Query);
        outerMiddlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Query);
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

        _ = services.AddConquerorQueryMiddleware<TestQueryMiddleware>(p => new(middlewareFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                                      middlewareLifetime);

        _ = services.AddConquerorQueryMiddleware<OuterTestQueryMiddleware>(p => new(outerMiddlewareFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                                           middlewareLifetime);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestQueryHandler>();
        _ = provider.GetRequiredService<TestQueryMiddleware>();
        _ = provider.GetRequiredService<OuterTestQueryMiddleware>();

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
                                                          IConquerorContext? ctx,
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
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly MiddlewareFn middlewareFn;

        public OuterTestQueryMiddleware(MiddlewareFn middlewareFn, IConquerorContextAccessor conquerorContextAccessor)
        {
            this.middlewareFn = middlewareFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, conquerorContextAccessor.ConquerorContext, async query =>
            {
                var response = await ctx.Next((query as TQuery)!, ctx.CancellationToken);
                return (response as TestQueryResponse)!;
            });
        }
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware
    {
        private readonly IConquerorContextAccessor conquerorContextAccessor;
        private readonly MiddlewareFn middlewareFn;

        public TestQueryMiddleware(MiddlewareFn middlewareFn, IConquerorContextAccessor conquerorContextAccessor)
        {
            this.middlewareFn = middlewareFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, conquerorContextAccessor.ConquerorContext, async query =>
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
