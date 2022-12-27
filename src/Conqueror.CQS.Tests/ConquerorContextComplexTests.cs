// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace Conqueror.CQS.Tests
{
    public sealed class ConquerorContextComplexTests
    {
        [Test]
        public async Task GivenQueryExecution_ConquerorContextIsAvailableInNestedCommandHandler()
        {
            var query = new TestQuery(10);

            var provider = Setup(nestedCommandHandlerFn: (cmd, ctx) =>
            {
                Assert.IsNotNull(ctx);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ConquerorContextIsAvailableInNestedQueryHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup(nestedQueryHandlerFn: (query, ctx) =>
            {
                Assert.IsNotNull(ctx);

                return new(query.Payload);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        [Combinatorial]
        public async Task GivenQueryExecution_ConquerorContextIsTheSameInNestedCommandHandlerRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedHandlerLifetime)
        {
            var query = new TestQuery(10);
            var observedContexts = new List<IConquerorContext>();

            var provider = Setup(
                queryHandlerFn: (_, ctx, next) =>
                {
                    observedContexts.Add(ctx!);
                    return next();
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    observedContexts.Add(ctx!);
                    return new(cmd.Payload);
                },
                queryHandlerLifetime: handlerLifetime,
                nestedCommandHandlerLifetime: nestedHandlerLifetime);

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            Assert.That(observedContexts, Has.Count.EqualTo(2));
            Assert.IsNotNull(observedContexts[0]);
            Assert.IsNotNull(observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
        }

        [Test]
        [Combinatorial]
        public async Task GivenCommandExecution_ConquerorContextIsTheSameInNestedCommandHandlerRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedHandlerLifetime)
        {
            var command = new TestCommand(10);
            var observedContexts = new List<IConquerorContext>();

            var provider = Setup(
                (_, ctx, next) =>
                {
                    observedContexts.Add(ctx!);
                    return next();
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    observedContexts.Add(ctx!);
                    return new(cmd.Payload);
                },
                queryHandlerLifetime: handlerLifetime,
                nestedCommandHandlerLifetime: nestedHandlerLifetime);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedContexts, Has.Count.EqualTo(2));
            Assert.IsNotNull(observedContexts[0]);
            Assert.IsNotNull(observedContexts[1]);
            Assert.AreSame(observedContexts[0], observedContexts[1]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsAreTheSameInNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var observedItems = new List<IDictionary<string, string>>();

            var provider = Setup(
                queryHandlerFn: (_, ctx, next) =>
                {
                    observedItems.Add(ctx!.Items);
                    return next();
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    observedItems.Add(ctx!.Items);
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(2));
            Assert.AreSame(observedItems[0], observedItems[1]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsAreTheSameInNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var observedItems = new List<IDictionary<string, string>>();

            var provider = Setup(
                (_, ctx, next) =>
                {
                    observedItems.Add(ctx!.Items);
                    return next();
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    observedItems.Add(ctx!.Items);
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(2));
            Assert.AreSame(observedItems[0], observedItems[1]);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSourceForNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var key = "key";
            var items = new Dictionary<string, string> { { key, "value" } };

            var provider = Setup(
                queryHandlerFn: (_, ctx, next) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return next();
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSourceForNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var items = new Dictionary<string, string> { { key, "value" } };

            var provider = Setup(
                (_, ctx, next) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return next();
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeSetForNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var key = "test";

            var provider = Setup(
                queryHandlerFn: (_, ctx, next) =>
                {
                    ctx!.Items[key] = key;
                    return next();
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeSetForNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var key = "test";

            var provider = Setup(
                (_, ctx, next) =>
                {
                    ctx!.Items[key] = key;
                    return next();
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    Assert.IsTrue(ctx?.Items.ContainsKey(key));
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeAddedFromSourceFromNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var key = "key";
            var items = new Dictionary<string, string> { { key, "value" } };

            var provider = Setup(
                queryHandlerFn: async (_, ctx, next) =>
                {
                    Assert.IsFalse(ctx!.Items.ContainsKey(key));
                    var response = await next();
                    Assert.IsTrue(ctx.Items.ContainsKey(key));
                    return response;
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSourceFromNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var items = new Dictionary<string, string> { { key, "value" } };

            var provider = Setup(
                async (_, ctx, next) =>
                {
                    Assert.IsFalse(ctx!.Items.ContainsKey(key));
                    var response = await next();
                    Assert.IsTrue(ctx.Items.ContainsKey(key));
                    return response;
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    ctx!.AddOrReplaceItems(items);
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenQueryExecution_ContextItemsCanBeSetFromNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var key = "test";

            var provider = Setup(
                queryHandlerFn: async (_, ctx, next) =>
                {
                    Assert.IsFalse(ctx!.Items.ContainsKey(key));
                    var response = await next();
                    Assert.IsTrue(ctx.Items.ContainsKey(key));
                    return response;
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeSetFromNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var key = "test";

            var provider = Setup(
                async (_, ctx, next) =>
                {
                    Assert.IsFalse(ctx!.Items.ContainsKey(key));
                    var response = await next();
                    Assert.IsTrue(ctx.Items.ContainsKey(key));
                    return response;
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    ctx!.Items[key] = key;
                    return new(cmd.Payload);
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItemsForQueryExecution_ContextItemsAreAvailableInNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var key = "key";
            var value = "value";

            var provider = Setup(nestedCommandHandlerFn: (cmd, ctx) =>
            {
                Assert.IsTrue(ctx!.Items.ContainsKey(key));
                Assert.AreEqual(value, ctx.Items[key]);
                return new(cmd.Payload);
            });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItemsForCommandExecution_ContextItemsAreAvailableInNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";

            var provider = Setup(nestedQueryHandlerFn: (cmd, ctx) =>
            {
                Assert.IsTrue(ctx!.Items.ContainsKey(key));
                Assert.AreEqual(value, ctx.Items[key]);
                return new(cmd.Payload);
            });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItemsForQueryExecution_ContextItemsCanBeChangedInHandlerForNestedCommandHandler()
        {
            var query = new TestQuery(10);
            var key = "key";
            var value = "value";
            var newValue = "newValue";

            var provider = Setup(
                queryHandlerFn: (_, ctx, next) =>
                {
                    Assert.AreEqual(value, ctx!.Items[key]);
                    ctx.Items[key] = newValue;
                    return next();
                },
                nestedCommandHandlerFn: (cmd, ctx) =>
                {
                    Assert.AreEqual(newValue, ctx!.Items[key]);
                    return new(cmd.Payload);
                });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItemsForCommandExecution_ContextItemsCanBeChangedInHandlerForNestedQueryHandler()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";
            var newValue = "newValue";

            var provider = Setup(
                (_, ctx, next) =>
                {
                    Assert.AreEqual(value, ctx!.Items[key]);
                    ctx.Items[key] = newValue;
                    return next();
                },
                nestedQueryHandlerFn: (cmd, ctx) =>
                {
                    Assert.AreEqual(newValue, ctx!.Items[key]);
                    return new(cmd.Payload);
                });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key] = value;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenManuallyCreatedContextForQueryExecution_ItemsFromNestedCommandHandlerAreAvailableInClientContext()
        {
            var query = new TestQuery(10);
            var key = "key";
            var value = "value";

            var provider = Setup(nestedCommandHandlerFn: (cmd, ctx) =>
            {
                ctx!.Items[key] = value;
                return new(cmd.Payload);
            });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            var contextItems = conquerorContext.Items;

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            Assert.AreEqual(value, contextItems[key]);
        }

        [Test]
        public async Task GivenManuallyCreatedContextForCommandExecution_ItemsFromNestedQueryHandlerAreAvailableInClientContext()
        {
            var command = new TestCommand(10);
            var key = "key";
            var value = "value";

            var provider = Setup(nestedQueryHandlerFn: (cmd, ctx) =>
            {
                ctx!.Items[key] = value;
                return new(cmd.Payload);
            });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            var contextItems = conquerorContext.Items;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.AreEqual(value, contextItems[key]);
        }

        [Test]
        public async Task GivenManuallyCreatedContextWithItemsForQueryExecution_ContextItemsFlowAcrossMultipleHandlerExecutions()
        {
            var query = new TestQuery(10);
            var command = new TestCommand(10);
            var invocationCount = 0;
            var key1 = "key 1";
            var key2 = "key 2";
            var value1 = "value 1";
            var value2 = "value 2";
            var value3 = "value 3";
            var value4 = "value 4";

            var provider = Setup(
                (_, ctx, next) =>
                {
                    if (invocationCount == 0)
                    {
                        Assert.AreEqual(value1, ctx!.Items[key1]);
                        Assert.IsFalse(ctx.Items.ContainsKey(key2));
                        ctx.Items[key2] = value2;
                    }
                    else if (invocationCount == 2)
                    {
                        Assert.AreEqual(value3, ctx!.Items[key1]);
                        Assert.AreEqual(value2, ctx.Items[key2]);
                        ctx.Items[key1] = value4;
                    }
                    else
                    {
                        Assert.Fail("should not reach this");
                    }

                    invocationCount += 1;

                    return next();
                },
                (_, ctx, next) =>
                {
                    if (invocationCount == 1)
                    {
                        Assert.AreEqual(value1, ctx!.Items[key1]);
                        Assert.AreEqual(value2, ctx.Items[key2]);
                        ctx.Items[key1] = value3;
                    }
                    else
                    {
                        Assert.Fail("should not reach this");
                    }

                    invocationCount += 1;

                    return next();
                });

            using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            conquerorContext.Items[key1] = value1;

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.AreSame(value4, conquerorContext.Items[key1]);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
        private IServiceProvider Setup(Func<TestCommand, IConquerorContext?, Func<Task<TestCommandResponse>>, Task<TestCommandResponse>>? commandHandlerFn = null,
                                       Func<TestQuery, IConquerorContext?, Func<Task<TestQueryResponse>>, Task<TestQueryResponse>>? queryHandlerFn = null,
                                       Func<NestedTestCommand, IConquerorContext?, NestedTestCommandResponse>? nestedCommandHandlerFn = null,
                                       Func<NestedTestQuery, IConquerorContext?, NestedTestQueryResponse>? nestedQueryHandlerFn = null,
                                       ServiceLifetime commandHandlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime queryHandlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime nestedCommandHandlerLifetime = ServiceLifetime.Transient,
                                       ServiceLifetime nestedQueryHandlerLifetime = ServiceLifetime.Transient)
        {
            commandHandlerFn ??= (_, _, next) => next();
            queryHandlerFn ??= (_, _, next) => next();
            nestedCommandHandlerFn ??= (cmd, _) => new(cmd.Payload);
            nestedQueryHandlerFn ??= (query, _) => new(query.Payload);

            var services = new ServiceCollection();

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandHandler),
                                                        p => new TestCommandHandler(commandHandlerFn,
                                                                                    p.GetRequiredService<IConquerorContextAccessor>(),
                                                                                    p.GetRequiredService<IQueryHandler<NestedTestQuery, NestedTestQueryResponse>>()),
                                                        commandHandlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedTestCommandHandler),
                                                        p => new NestedTestCommandHandler(nestedCommandHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                        nestedCommandHandlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestQueryHandler),
                                                        p => new TestQueryHandler(queryHandlerFn,
                                                                                  p.GetRequiredService<IConquerorContextAccessor>(),
                                                                                  p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()),
                                                        queryHandlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedTestQueryHandler),
                                                        p => new NestedTestQueryHandler(nestedQueryHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()),
                                                        nestedQueryHandlerLifetime));

            var provider = services.AddConquerorCQS().ConfigureConqueror().BuildServiceProvider();

            _ = provider.GetRequiredService<TestCommandHandler>();
            _ = provider.GetRequiredService<NestedTestCommandHandler>();
            _ = provider.GetRequiredService<TestQueryHandler>();
            _ = provider.GetRequiredService<NestedTestQueryHandler>();

            return provider.CreateScope().ServiceProvider;
        }

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int Payload);

        private sealed record NestedTestCommand(int Payload);

        private sealed record NestedTestCommandResponse(int Payload);

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly Func<TestCommand, IConquerorContext?, Func<Task<TestCommandResponse>>, Task<TestCommandResponse>> handlerFn;
            private readonly IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler;

            public TestCommandHandler(Func<TestCommand, IConquerorContext?, Func<Task<TestCommandResponse>>, Task<TestCommandResponse>> handlerFn,
                                      IConquerorContextAccessor conquerorContextAccessor,
                                      IQueryHandler<NestedTestQuery, NestedTestQueryResponse> nestedQueryHandler)
            {
                this.handlerFn = handlerFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.nestedQueryHandler = nestedQueryHandler;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return await handlerFn(query, conquerorContextAccessor.ConquerorContext, async () =>
                {
                    var response = await nestedQueryHandler.ExecuteQuery(new(query.Payload), cancellationToken);
                    return new(response.Payload);
                });
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

            public async Task<NestedTestCommandResponse> ExecuteCommand(NestedTestCommand query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return handlerFn(query, conquerorContextAccessor.ConquerorContext);
            }
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed record NestedTestQuery(int Payload);

        private sealed record NestedTestQueryResponse(int Payload);

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly IConquerorContextAccessor conquerorContextAccessor;
            private readonly Func<TestQuery, IConquerorContext?, Func<Task<TestQueryResponse>>, Task<TestQueryResponse>> handlerFn;
            private readonly ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler;

            public TestQueryHandler(Func<TestQuery, IConquerorContext?, Func<Task<TestQueryResponse>>, Task<TestQueryResponse>> handlerFn,
                                    IConquerorContextAccessor conquerorContextAccessor,
                                    ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler)
            {
                this.handlerFn = handlerFn;
                this.conquerorContextAccessor = conquerorContextAccessor;
                this.nestedCommandHandler = nestedCommandHandler;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return await handlerFn(query, conquerorContextAccessor.ConquerorContext, async () =>
                {
                    var response = await nestedCommandHandler.ExecuteCommand(new(query.Payload), cancellationToken);
                    return new(response.Payload);
                });
            }
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
    }
}
