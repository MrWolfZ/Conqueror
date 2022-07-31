using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Conqueror.CQS.Tests
{
    public sealed class ConquerorContextCommandTests
    {
        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup((cmd, ctx) =>
            {
                Assert.IsNotNull(ctx);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandlerWithoutResponse()
        {
            var command = new TestCommandWithoutResponse(10);

            var provider = SetupWithoutResponse((_, ctx) => { Assert.IsNotNull(ctx); });

            await provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInMiddleware()
        {
            var command = new TestCommand(10);
            var response = new TestCommandResponse(11);

            var provider = Setup((_, _) => response, middlewareFn: async (middlewareCtx, ctx, next) =>
            {
                Assert.IsNotNull(ctx);

                return await next(middlewareCtx.Command);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInNestedClass()
        {
            var command = new TestCommand(10);

            var provider = Setup(
                nestedClassFn: Assert.IsNotNull,
                nestedClassLifetime: ServiceLifetime.Scoped);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInNestedHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
            {
                Assert.IsNotNull(ctx);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandlerAfterExecutionOfNestedHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup(handlerPreReturnFn: Assert.IsNotNull);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        [Combinatorial]
        public async Task GivenCommandExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime middlewareLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedClassLifetime)
        {
            var command = new TestCommand(10);
            var observedContexts = new List<IConquerorContext>();

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
                    return next(middlewareCtx.Command);
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Command),
                ctx => observedContexts.Add(ctx!),
                _ => { },
                handlerLifetime,
                middlewareLifetime,
                nestedClassLifetime);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedContexts, Has.Count.EqualTo(3));
            Assert.IsNotNull(observedContexts[0]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[2]);
        }

        [Test]
        [Combinatorial]
        public async Task GivenCommandExecution_ConquerorContextIsTheSameInNestedHandlerRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedHandlerLifetime)
        {
            var command = new TestCommand(10);
            var observedContexts = new List<IConquerorContext>();

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

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedContexts, Has.Count.EqualTo(2));
            Assert.IsNotNull(observedContexts[0]);
            Assert.IsNotNull(observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassWithConfigureAwait()
        {
            var command = new TestCommand(10);
            var observedContexts = new List<IConquerorContext>();

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
                    return await next(middlewareCtx.Command);
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Command),
                ctx => observedContexts.Add(ctx!));

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedContexts, Has.Count.EqualTo(3));
            Assert.IsNotNull(observedContexts[0]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[2]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsAreTheSameInHandlerMiddlewareAndNestedClass()
        {
            var command = new TestCommand(10);
            var observedItems = new List<IDictionary<string, string>>();

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
                    return next(middlewareCtx.Command);
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Command),
                ctx => observedItems.Add(ctx!.Items));

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(3));
            Assert.AreSame(observedItems[0], observedItems[1]);
            Assert.AreSame(observedItems[0], observedItems[2]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsAreTheSameInNestedHandler()
        {
            var command = new TestCommand(10);
            var observedItems = new List<IDictionary<string, string>>();

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

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(2));
            Assert.AreSame(observedItems[0], observedItems[1]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSource()
        {
            var command = new TestCommand(10);
            var items = new Dictionary<string, string> { { "key", "value" } };
            var observedKeys = new List<string>();

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
                    var r = await next(middlewareCtx.Command);
                    observedKeys.AddRange(ctx.Items.Keys);
                    return r;
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Command),
                ctx => observedKeys.AddRange(ctx!.Items.Keys));

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedKeys, Has.Count.EqualTo(2));
            Assert.AreSame(observedKeys[0], observedKeys[1]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSourceWithDuplicates()
        {
            var command = new TestCommand(10);
            var items = new Dictionary<string, string> { { "key", "value" } };
            var observedKeys = new List<string>();

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
                    var r = await next(middlewareCtx.Command);
                    observedKeys.AddRange(ctx.Items.Keys);
                    return r;
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Command),
                ctx => observedKeys.AddRange(ctx!.Items.Keys));

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedKeys, Has.Count.EqualTo(2));
            Assert.AreSame(observedKeys[0], observedKeys[1]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeSet()
        {
            var command = new TestCommand(10);
            var key = "test";
            var observedItems = new List<string?>();

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
                    var r = await next(middlewareCtx.Command);
                    observedItems.Add(ctx.Items.TryGetValue(key, out var k2) ? k2 : null);
                    return r;
                },
                (middlewareCtx, _, next) => next(middlewareCtx.Command),
                ctx => observedItems.Add(ctx!.Items.TryGetValue(key, out var k) ? k : null));

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(3));
            Assert.IsNull(observedItems[0]);
            Assert.AreSame(key, observedItems[1]);
            Assert.AreSame(key, observedItems[2]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSourceForNestedHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var items = new Dictionary<string, string> { { key, "value" } };

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeSetForNestedHandler()
        {
            var command = new TestCommand(10);
            var key = "test";

            var provider = Setup(
                (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSourceFromNestedHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var items = new Dictionary<string, string> { { key, "value" } };

            var provider = Setup(
                nestedHandlerFn: (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                },
                middlewareFn: async (middlewareCtx, ctx, next) =>
                {
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    var r = await next(middlewareCtx.Command);
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return r;
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeSetFromNestedHandler()
        {
            var command = new TestCommand(10);
            var key = "test";

            var provider = Setup(
                nestedHandlerFn: (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                },
                middlewareFn: async (middlewareCtx, ctx, next) =>
                {
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    var r = await next(middlewareCtx.Command);
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return r;
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public void GivenNoCommandExecution_ConquerorContextIsNotAvailable()
        {
            var services = new ServiceCollection().AddConquerorCQS();

            _ = services.AddTransient(p => new NestedClass(Assert.IsNull, p.GetRequiredService<IConquerorContextAccessor>()));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            provider.GetRequiredService<NestedClass>().Execute();
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItems_ContextItemsAreAvailableInHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";

            var provider = Setup((cmd, ctx) =>
            {
                Assert.IsTrue(ctx!.Items.ContainsKey(key));
                Assert.AreEqual(value, ctx.Items[key]);
                return new(cmd.Payload);
            });

            var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItems_ContextItemsAreAvailableInNestedHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";

            var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
            {
                Assert.IsTrue(ctx!.Items.ContainsKey(key));
                Assert.AreEqual(value, ctx.Items[key]);
                return new(cmd.Payload);
            });

            var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItems_ContextItemsCanBeChangedInHandlerForNestedHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";
            var newValue = "newValue";

            var provider = Setup(
                (cmd, ctx) =>
                {
                    Assert.AreEqual(value, ctx!.Items[key]);
                    ctx.Items[key] = newValue;
                    return new(cmd.Payload);
                },
                (cmd, ctx) =>
                {
                    Assert.AreEqual(newValue, ctx!.Items[key]);
                    return new(cmd.Payload);
                });

            var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenClientContext_ItemsFromHandlerAreAvailableInClientContext()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";

            var provider = Setup((cmd, ctx) =>
            {
                ctx!.Items[key] = value;
                return new(cmd.Payload);
            });

            var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            var contextItems = conquerorContext.Items;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.AreEqual(value, contextItems[key]);
        }

        [Test]
        public async Task GivenClientContext_ItemsFromNestedHandlerAreAvailableInClientContext()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";

            var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
            {
                ctx!.Items[key] = value;
                return new(cmd.Payload);
            });

            var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            var contextItems = conquerorContext.Items;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.AreEqual(value, contextItems[key]);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItems_ContextItemsFlowAcrossMultipleHandlerExecutions()
        {
            var command = new TestCommand(10);
            var invocationCount = 0;
            var key1 = "key 1";
            var key2 = "key 2";
            var value1 = "value 1";
            var value2 = "value 2";
            var value3 = "value 3";

            var provider = Setup((cmd, ctx) =>
            {
                if (invocationCount == 0)
                {
                    Assert.AreEqual(value1, ctx!.Items[key1]);
                    Assert.IsFalse(ctx.Items.ContainsKey(key2));
                    ctx.Items[key2] = value2;
                }
                else if (invocationCount == 1)
                {
                    Assert.AreEqual(value1, ctx!.Items[key1]);
                    Assert.AreEqual(value2, ctx.Items[key2]);
                    ctx.Items[key1] = value3;
                }
                else if (invocationCount == 2)
                {
                    Assert.AreEqual(value3, ctx!.Items[key1]);
                    Assert.AreEqual(value2, ctx.Items[key2]);
                }

                invocationCount += 1;

                return new(cmd.Payload);
            });

            var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key1] = value1;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
        private IServiceProvider Setup(Func<TestCommand, IConquerorContext?, TestCommandResponse>? handlerFn = null,
                                       Func<NestedTestCommand, IConquerorContext?, NestedTestCommandResponse>? nestedHandlerFn = null,
                                       MiddlewareFn? middlewareFn = null,
                                       MiddlewareFn? outerMiddlewareFn = null,
                                       Action<IConquerorContext?>? nestedClassFn = null,
                                       Action<IConquerorContext?>? handlerPreReturnFn = null,
                                       ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime nestedHandlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
        {
            handlerFn ??= (cmd, _) => new(cmd.Payload);
            nestedHandlerFn ??= (cmd, _) => new(cmd.Payload);
            middlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Command);
            outerMiddlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Command);
            nestedClassFn ??= _ => { };
            handlerPreReturnFn ??= _ => { };

            var services = new ServiceCollection();

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedClassLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandHandler),
                                                        p => new TestCommandHandler(handlerFn,
                                                                                    handlerPreReturnFn,
                                                                                    p.GetRequiredService<IConquerorContextAccessor>(),
                                                                                    p.GetRequiredService<NestedClass>(),
                                                                                    p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()),
                                                        handlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedTestCommandHandler),
                                                        p => new NestedTestCommandHandler(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                        nestedHandlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandMiddleware),
                                                        p => new TestCommandMiddleware(middlewareFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                        middlewareLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(OuterTestCommandMiddleware),
                                                        p => new OuterTestCommandMiddleware(outerMiddlewareFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                        middlewareLifetime));

            var provider = services.AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            _ = provider.GetRequiredService<NestedClass>();
            _ = provider.GetRequiredService<TestCommandHandler>();
            _ = provider.GetRequiredService<TestCommandMiddleware>();
            _ = provider.GetRequiredService<OuterTestCommandMiddleware>();

            return provider.CreateScope().ServiceProvider;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
        private IServiceProvider SetupWithoutResponse(Action<TestCommandWithoutResponse, IConquerorContext?>? handlerFn = null,
                                                      MiddlewareFn? middlewareFn = null,
                                                      Action<IConquerorContext?>? nestedClassFn = null,
                                                      ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
                                                      ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                                      ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
        {
            handlerFn ??= (_, _) => { };
            middlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Command);
            nestedClassFn ??= _ => { };

            var services = new ServiceCollection();

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedClassLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandHandlerWithoutResponse),
                                                        p => new TestCommandHandlerWithoutResponse(handlerFn, p.GetRequiredService<IConquerorContextAccessor>(), p.GetRequiredService<NestedClass>()),
                                                        handlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandMiddleware),
                                                        p => new TestCommandMiddleware(middlewareFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                        middlewareLifetime));

            var provider = services.AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            _ = provider.GetRequiredService<NestedClass>();
            _ = provider.GetRequiredService<TestCommandHandlerWithoutResponse>();
            _ = provider.GetRequiredService<TestCommandMiddleware>();

            return provider.CreateScope().ServiceProvider;
        }

        private delegate Task<TestCommandResponse> MiddlewareFn(CommandMiddlewareContext<TestCommand, TestCommandResponse> middlewareCtx,
                                                                IConquerorContext? ctx,
                                                                Func<TestCommand, Task<TestCommandResponse>> next);

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int Payload);

        private sealed record TestCommandWithoutResponse(int Payload);

        private sealed record NestedTestCommand(int Payload);

        private sealed record NestedTestCommandResponse(int Payload);

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly Func<TestCommand, IConquerorContext?, TestCommandResponse> handlerFn;
            private readonly NestedClass nestedClass;
            private readonly ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler;
            private readonly Action<IConquerorContext?> preReturnFn;

            public TestCommandHandler(Func<TestCommand, IConquerorContext?, TestCommandResponse> handlerFn,
                                      Action<IConquerorContext?> preReturnFn,
                                      IConquerorContextAccessor conquerorContextAccessor,
                                      NestedClass nestedClass,
                                      ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler)
            {
                this.handlerFn = handlerFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.nestedClass = nestedClass;
                this.nestedCommandHandler = nestedCommandHandler;
                this.preReturnFn = preReturnFn;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                var response = handlerFn(command, conquerorContextAccessor.ConquerorContext);
                nestedClass.Execute();
                _ = await nestedCommandHandler.ExecuteCommand(new(command.Payload), cancellationToken);
                preReturnFn(conquerorContextAccessor.ConquerorContext);
                return response;
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline.Use<OuterTestCommandMiddleware>()
                                                                                              .Use<TestCommandMiddleware>();
        }

        private sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly Action<TestCommandWithoutResponse, IConquerorContext?> handlerFn;
            private readonly NestedClass nestedClass;

            public TestCommandHandlerWithoutResponse(Action<TestCommandWithoutResponse, IConquerorContext?> handlerFn, IConquerorContextAccessor conquerorContextAccessor, NestedClass nestedClass)
            {
                this.handlerFn = handlerFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.nestedClass = nestedClass;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                handlerFn(command, conquerorContextAccessor.ConquerorContext);
                nestedClass.Execute();
            }
        }

        private sealed class NestedTestCommandHandler : ICommandHandler<NestedTestCommand, NestedTestCommandResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly Func<NestedTestCommand, IConquerorContext?, NestedTestCommandResponse> handlerFn;

            public NestedTestCommandHandler(Func<NestedTestCommand, IConquerorContext?, NestedTestCommandResponse> handlerFn, IConquerorContextAccessor conquerorContextAccessor)
            {
                this.handlerFn = handlerFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
            }

            public async Task<NestedTestCommandResponse> ExecuteCommand(NestedTestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return handlerFn(command, conquerorContextAccessor.ConquerorContext);
            }
        }

        private sealed class OuterTestCommandMiddleware : ICommandMiddleware
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly MiddlewareFn middlewareFn;

            public OuterTestCommandMiddleware(MiddlewareFn middlewareFn, IConquerorContextAccessor conquerorContextAccessor)
            {
                this.middlewareFn = middlewareFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, conquerorContextAccessor.ConquerorContext, async cmd =>
                {
                    var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                    return (response as TestCommandResponse)!;
                });
            }
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly MiddlewareFn middlewareFn;

            public TestCommandMiddleware(MiddlewareFn middlewareFn, IConquerorContextAccessor conquerorContextAccessor)
            {
                this.middlewareFn = middlewareFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, conquerorContextAccessor.ConquerorContext, async cmd =>
                {
                    var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                    return (response as TestCommandResponse)!;
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
}
