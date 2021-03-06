using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class QueryContextTests
    {
        [Test]
        public async Task GivenQueryExecution_QueryContextIsAvailableInHandler()
        {
            var query = new TestQuery(10);

            var provider = Setup((cmd, ctx) =>
            {
                Assert.AreEqual(query, cmd);
                Assert.IsNotNull(ctx);
                Assert.AreEqual(query, ctx!.Query);
                Assert.IsNull(ctx.Response);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextIsAvailableInMiddleware()
        {
            var query = new TestQuery(10);
            var response = new TestQueryResponse(11);

            var provider = Setup((_, _) => response, middlewareFn: async (middlewareCtx, ctx, next) =>
            {
                Assert.AreEqual(query, middlewareCtx.Query);
                Assert.IsNotNull(ctx);
                Assert.AreEqual(query, ctx!.Query);
                Assert.IsNull(ctx.Response);

                var resp = await next(middlewareCtx.Query);

                Assert.AreEqual(response, resp);
                Assert.AreEqual(response, ctx.Response);

                return resp;
            });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextIsAvailableInNestedClass()
        {
            var query = new TestQuery(10);

            var provider = Setup(
                nestedClassFn: ctx =>
                {
                    Assert.IsNotNull(ctx);
                    Assert.AreEqual(query, ctx!.Query);
                    Assert.IsNull(ctx.Response);
                },
                nestedClassLifetime: ServiceLifetime.Scoped);

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextIsAvailableInNestedHandler()
        {
            var query = new TestQuery(10);

            var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
            {
                Assert.IsNotNull(ctx);
                Assert.AreNotEqual(query, ctx!.Query);
                Assert.IsNull(ctx.Response);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextIsAvailableInHandlerAfterExecutionOfNestedHandler()
        {
            var query = new TestQuery(10);

            var provider = Setup(handlerPreReturnFn: ctx =>
            {
                Assert.IsNotNull(ctx);
                Assert.AreEqual(query, ctx!.Query);
            });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextReturnsCurrentQueryIfQueryIsChangedInMiddleware()
        {
            var query = new TestQuery(10);
            var modifiedQuery = new TestQuery(11);
            var response = new TestQueryResponse(11);

            var provider = Setup(
                (_, ctx) =>
                {
                    Assert.AreSame(modifiedQuery, ctx!.Query);
                    return response;
                },
                middlewareFn: async (_, _, next) => await next(modifiedQuery));

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextReturnsCurrentResponseIfResponseIsChangedInMiddleware()
        {
            var query = new TestQuery(10);
            var response = new TestQueryResponse(11);
            var modifiedResponse = new TestQueryResponse(12);

            var provider = Setup(
                (_, _) => response,
                middlewareFn: async (middlewareCtx, _2, next) =>
                {
                    _ = await next(middlewareCtx.Query);
                    return modifiedResponse;
                },
                outerMiddlewareFn: async (middlewareCtx, ctx, next) =>
                {
                    var result = await next(middlewareCtx.Query);
                    Assert.AreSame(modifiedResponse, ctx!.Response);
                    return result;
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        [Combinatorial]
        public async Task GivenQueryExecution_QueryContextIsTheSameInMiddlewareHandlerAndNestedClassRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime middlewareLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedClassLifetime)
        {
            var query = new TestQuery(10);
            var observedContexts = new List<IQueryContext>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    observedContexts.Add(ctx!);
                    return new(cmd.Payload);
                },
                (cmd, _) => new(cmd.Payload),
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
            Assert.IsNotNull(observedContexts[0]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[2]);
        }

        [Test]
        [Combinatorial]
        public async Task GivenQueryExecution_QueryContextIsNotTheSameInNestedHandlerRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedHandlerLifetime)
        {
            var query = new TestQuery(10);
            var observedContexts = new List<IQueryContext>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    observedContexts.Add(ctx!);
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    observedContexts.Add(ctx!);
                    return new(cmd.Payload);
                },
                handlerLifetime: handlerLifetime,
                nestedHandlerLifetime: nestedHandlerLifetime);

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            Assert.That(observedContexts, Has.Count.EqualTo(2));
            Assert.IsNotNull(observedContexts[0]);
            Assert.IsNotNull(observedContexts[1]);
            Assert.AreNotSame(observedContexts[0], observedContexts[1]);
        }

        [Test]
        public async Task GivenQueryExecution_QueryContextIsTheSameInMiddlewareHandlerAndNestedClassWithConfigureAwait()
        {
            var query = new TestQuery(10);
            var observedContexts = new List<IQueryContext>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    observedContexts.Add(ctx!);
                    return new(cmd.Payload);
                },
                (cmd, _) => new(cmd.Payload),
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
            Assert.IsNotNull(observedContexts[0]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[2]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsAreTheSameInHandlerMiddlewareAndNestedClass()
        {
            var query = new TestQuery(10);
            var observedItems = new List<IDictionary<object, object?>>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    observedItems.Add(ctx!.Items);
                    return new(cmd.Payload);
                },
                (cmd, _) => new(cmd.Payload),
                (middlewareCtx, ctx, next) =>
                {
                    observedItems.Add(ctx!.Items);
                    return next(middlewareCtx.Query);
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Query),
                ctx => observedItems.Add(ctx!.Items));

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(3));
            Assert.AreSame(observedItems[0], observedItems[1]);
            Assert.AreSame(observedItems[0], observedItems[2]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsAreNotTheSameInNestedHandler()
        {
            var query = new TestQuery(10);
            var observedItems = new List<IDictionary<object, object?>>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    observedItems.Add(ctx!.Items);
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    observedItems.Add(ctx!.Items);
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(2));
            Assert.AreNotSame(observedItems[0], observedItems[1]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSource()
        {
            var query = new TestQuery(10);
            var items = new Dictionary<object, object?> { { new object(), new object() } };
            var observedKeys = new List<object>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                },
                (cmd, _) => new(cmd.Payload),
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
            Assert.AreSame(observedKeys[0], observedKeys[1]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSourceWithDuplicates()
        {
            var query = new TestQuery(10);
            var items = new Dictionary<object, object?> { { new object(), new object() } };
            var observedKeys = new List<object>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                },
                (cmd, _) => new(cmd.Payload),
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
            Assert.AreSame(observedKeys[0], observedKeys[1]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeSet()
        {
            var query = new TestQuery(10);
            var key = new object();
            var observedItems = new List<object?>();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                },
                (cmd, _) => new(cmd.Payload),
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
            Assert.IsNull(observedItems[0]);
            Assert.AreSame(key, observedItems[1]);
            Assert.AreSame(key, observedItems[2]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCannotBeAddedFromSourceForNestedHandler()
        {
            var query = new TestQuery(10);
            var key = new object();
            var items = new Dictionary<object, object?> { { key, new object() } };

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCannotBeSetForNestedHandler()
        {
            var query = new TestQuery(10);
            var key = new object();

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCannotBeAddedFromSourceFromNestedHandler()
        {
            var query = new TestQuery(10);
            var key = new object();
            var items = new Dictionary<object, object?> { { key, new object() } };

            var provider = Setup(
                nestedHandlerFn: (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                },
                middlewareFn: async (middlewareCtx, ctx, next) =>
                {
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    var r = await next(middlewareCtx.Query);
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    return r;
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCannotBeSetFromNestedHandler()
        {
            var query = new TestQuery(10);
            var key = new object();

            var provider = Setup(
                nestedHandlerFn: (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                },
                middlewareFn: async (middlewareCtx, ctx, next) =>
                {
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    var r = await next(middlewareCtx.Query);
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    return r;
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
        private IServiceProvider Setup(Func<TestQuery, IQueryContext?, TestQueryResponse>? handlerFn = null,
                                       Func<NestedTestQuery, IQueryContext?, NestedTestQueryResponse>? nestedHandlerFn = null,
                                       MiddlewareFn? middlewareFn = null,
                                       MiddlewareFn? outerMiddlewareFn = null,
                                       Action<IQueryContext?>? nestedClassFn = null,
                                       Action<IQueryContext?>? handlerPreReturnFn = null,
                                       ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime nestedHandlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
        {
            handlerFn ??= (cmd, _) => new(cmd.Payload);
            nestedHandlerFn ??= (cmd, _) => new(cmd.Payload);
            middlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Query);
            outerMiddlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Query);
            nestedClassFn ??= _ => { };
            handlerPreReturnFn ??= _ => { };

            var services = new ServiceCollection();

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IQueryContextAccessor>()), nestedClassLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestQueryHandler),
                                                        p => new TestQueryHandler(handlerFn,
                                                                                  handlerPreReturnFn,
                                                                                  p.GetRequiredService<IQueryContextAccessor>(),
                                                                                  p.GetRequiredService<NestedClass>(),
                                                                                  p.GetRequiredService<IQueryHandler<NestedTestQuery, NestedTestQueryResponse>>()),
                                                        handlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedTestQueryHandler),
                                                        p => new NestedTestQueryHandler(nestedHandlerFn, p.GetRequiredService<IQueryContextAccessor>()),
                                                        nestedHandlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestQueryMiddleware),
                                                        p => new TestQueryMiddleware(middlewareFn, p.GetRequiredService<IQueryContextAccessor>()),
                                                        middlewareLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(OuterTestQueryMiddleware),
                                                        p => new OuterTestQueryMiddleware(outerMiddlewareFn, p.GetRequiredService<IQueryContextAccessor>()),
                                                        middlewareLifetime));

            var provider = services.AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            _ = provider.GetRequiredService<NestedClass>();
            _ = provider.GetRequiredService<TestQueryHandler>();
            _ = provider.GetRequiredService<TestQueryMiddleware>();
            _ = provider.GetRequiredService<OuterTestQueryMiddleware>();

            return provider.CreateScope().ServiceProvider;
        }

        private delegate Task<TestQueryResponse> MiddlewareFn(QueryMiddlewareContext<TestQuery, TestQueryResponse> middlewareCtx,
                                                              IQueryContext? ctx,
                                                              Func<TestQuery, Task<TestQueryResponse>> next);

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed record NestedTestQuery(int Payload);

        private sealed record NestedTestQueryResponse(int Payload);

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly Func<TestQuery, IQueryContext?, TestQueryResponse> handlerFn;
            private readonly NestedClass nestedClass;
            private readonly IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler;
            private readonly Action<IQueryContext?> preReturnFn;
            private readonly IQueryContextAccessor queryContextAccessor;

            public TestQueryHandler(Func<TestQuery, IQueryContext?, TestQueryResponse> handlerFn,
                                    Action<IQueryContext?> preReturnFn,
                                    IQueryContextAccessor queryContextAccessor,
                                    NestedClass nestedClass,
                                    IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler)
            {
                this.handlerFn = handlerFn;
                this.queryContextAccessor = queryContextAccessor;
                this.nestedClass = nestedClass;
                this.nestedQueryHandler = nestedQueryHandler;
                this.preReturnFn = preReturnFn;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                var response = handlerFn(query, queryContextAccessor.QueryContext);
                nestedClass.Execute();
                _ = await nestedQueryHandler.ExecuteQuery(new(query.Payload), cancellationToken);
                preReturnFn(queryContextAccessor.QueryContext);
                return response;
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline) => pipeline.Use<OuterTestQueryMiddleware>()
                                                                                            .Use<TestQueryMiddleware>();
        }

        private sealed class NestedTestQueryHandler : IQueryHandler<NestedTestQuery, NestedTestQueryResponse>
        {
            private readonly Func<NestedTestQuery, IQueryContext?, NestedTestQueryResponse> handlerFn;
            private readonly IQueryContextAccessor queryContextAccessor;

            public NestedTestQueryHandler(Func<NestedTestQuery, IQueryContext?, NestedTestQueryResponse> handlerFn, IQueryContextAccessor queryContextAccessor)
            {
                this.handlerFn = handlerFn;
                this.queryContextAccessor = queryContextAccessor;
            }

            public async Task<NestedTestQueryResponse> ExecuteQuery(NestedTestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return handlerFn(query, queryContextAccessor.QueryContext);
            }
        }

        private sealed class OuterTestQueryMiddleware : IQueryMiddleware
        {
            private readonly MiddlewareFn middlewareFn;
            private readonly IQueryContextAccessor queryContextAccessor;

            public OuterTestQueryMiddleware(MiddlewareFn middlewareFn, IQueryContextAccessor queryContextAccessor)
            {
                this.middlewareFn = middlewareFn;
                this.queryContextAccessor = queryContextAccessor;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, queryContextAccessor.QueryContext, async cmd =>
                {
                    var response = await ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
                    return (response as TestQueryResponse)!;
                });
            }
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware
        {
            private readonly MiddlewareFn middlewareFn;
            private readonly IQueryContextAccessor queryContextAccessor;

            public TestQueryMiddleware(MiddlewareFn middlewareFn, IQueryContextAccessor queryContextAccessor)
            {
                this.middlewareFn = middlewareFn;
                this.queryContextAccessor = queryContextAccessor;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                return (TResponse)(object)await middlewareFn((ctx as QueryMiddlewareContext<TestQuery, TestQueryResponse>)!, queryContextAccessor.QueryContext, async cmd =>
                {
                    var response = await ctx.Next((cmd as TQuery)!, ctx.CancellationToken);
                    return (response as TestQueryResponse)!;
                });
            }
        }

        private sealed class NestedClass
        {
            private readonly Action<IQueryContext?> nestedClassFn;
            private readonly IQueryContextAccessor queryContextAccessor;

            public NestedClass(Action<IQueryContext?> nestedClassFn, IQueryContextAccessor queryContextAccessor)
            {
                this.nestedClassFn = nestedClassFn;
                this.queryContextAccessor = queryContextAccessor;
            }

            public void Execute()
            {
                nestedClassFn(queryContextAccessor.QueryContext);
            }
        }
    }
}
