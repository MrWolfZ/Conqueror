namespace Conqueror.CQS.Tests
{
    public sealed class CommandContextTests
    {
        [Test]
        public async Task GivenCommandExecution_CommandContextIsAvailableInHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup((cmd, ctx) =>
            {
                Assert.AreEqual(command, cmd);
                Assert.IsNotNull(ctx);
                Assert.AreEqual(command, ctx!.Command);
                Assert.IsNull(ctx.Response);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextIsAvailableInHandlerWithoutResponse()
        {
            var command = new TestCommandWithoutResponse(10);

            var provider = SetupWithoutResponse((cmd, ctx) =>
            {
                Assert.AreEqual(command, cmd);
                Assert.IsNotNull(ctx);
                Assert.AreEqual(command, ctx!.Command);
                Assert.IsNull(ctx.Response);
            });

            await provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextIsAvailableInMiddleware()
        {
            var command = new TestCommand(10);
            var response = new TestCommandResponse(11);

            var provider = Setup((_, _) => response, middlewareFn: async (middlewareCtx, ctx, next) =>
            {
                Assert.AreEqual(command, middlewareCtx.Command);
                Assert.IsNotNull(ctx);
                Assert.AreEqual(command, ctx!.Command);
                Assert.IsNull(ctx.Response);

                var resp = await next(middlewareCtx.Command);

                Assert.AreEqual(response, resp);
                Assert.AreEqual(response, ctx.Response);

                return resp;
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextIsAvailableInNestedClass()
        {
            var command = new TestCommand(10);

            var provider = Setup(
                nestedClassFn: ctx =>
                {
                    Assert.IsNotNull(ctx);
                    Assert.AreEqual(command, ctx!.Command);
                    Assert.IsNull(ctx.Response);
                },
                nestedClassLifetime: ServiceLifetime.Scoped);

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextIsAvailableInNestedHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
            {
                Assert.IsNotNull(ctx);
                Assert.AreNotEqual(command, ctx!.Command);
                Assert.IsNull(ctx.Response);

                return new(cmd.Payload);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextIsAvailableInHandlerAfterExecutionOfNestedHandler()
        {
            var command = new TestCommand(10);

            var provider = Setup(handlerPreReturnFn: ctx =>
            {
                Assert.IsNotNull(ctx);
                Assert.AreEqual(command, ctx!.Command);
            });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextReturnsCurrentCommandIfCommandIsChangedInMiddleware()
        {
            var command = new TestCommand(10);
            var modifiedCommand = new TestCommand(11);
            var response = new TestCommandResponse(11);

            var provider = Setup(
                (_, ctx) =>
                {
                    Assert.AreSame(modifiedCommand, ctx!.Command);
                    return response;
                },
                middlewareFn: async (_, _, next) => await next(modifiedCommand));

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextReturnsCurrentResponseIfResponseIsChangedInMiddleware()
        {
            var command = new TestCommand(10);
            var response = new TestCommandResponse(11);
            var modifiedResponse = new TestCommandResponse(12);

            var provider = Setup(
                (_, _) => response,
                middlewareFn: async (middlewareCtx, _2, next) =>
                {
                    _ = await next(middlewareCtx.Command);
                    return modifiedResponse;
                },
                outerMiddlewareFn: async (middlewareCtx, ctx, next) =>
                {
                    var result = await next(middlewareCtx.Command);
                    Assert.AreSame(modifiedResponse, ctx!.Response);
                    return result;
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        [Combinatorial]
        public async Task GivenCommandExecution_CommandContextIsTheSameInMiddlewareHandlerAndNestedClassRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime middlewareLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedClassLifetime)
        {
            var command = new TestCommand(10);
            var observedContexts = new List<ICommandContext>();

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
        public async Task GivenCommandExecution_CommandContextIsNotTheSameInNestedHandlerRegardlessOfLifetime(
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime handlerLifetime,
            [Values(ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
            ServiceLifetime nestedHandlerLifetime)
        {
            var command = new TestCommand(10);
            var observedContexts = new List<ICommandContext>();

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
            Assert.AreNotSame(observedContexts[0], observedContexts[1]);
        }

        [Test]
        public async Task GivenCommandExecution_CommandContextIsTheSameInMiddlewareHandlerAndNestedClassWithConfigureAwait()
        {
            var command = new TestCommand(10);
            var observedContexts = new List<ICommandContext>();

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
        public async Task GivenCommandExecution_ContextItemsAreNotTheSameInNestedHandler()
        {
            var command = new TestCommand(10);
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

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

            Assert.That(observedItems, Has.Count.EqualTo(2));
            Assert.AreNotSame(observedItems[0], observedItems[1]);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCanBeAddedFromSource()
        {
            var command = new TestCommand(10);
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
        public async Task GivenCommandExecution_ContextItemsCannotBeAddedFromSourceForNestedHandler()
        {
            var command = new TestCommand(10);
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

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCannotBeSetForNestedHandler()
        {
            var command = new TestCommand(10);
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

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCannotBeAddedFromSourceFromNestedHandler()
        {
            var command = new TestCommand(10);
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
                    var r = await next(middlewareCtx.Command);
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    return r;
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [Test]
        public async Task GivenCommandExecution_ContextItemsCannotBeSetFromNestedHandler()
        {
            var command = new TestCommand(10);
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
                    var r = await next(middlewareCtx.Command);
                    Assert.IsFalse(ctx?.Items.ContainsKey(key));
                    return r;
                });

            _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
        private IServiceProvider Setup(Func<TestCommand, ICommandContext?, TestCommandResponse>? handlerFn = null,
                                       Func<NestedTestCommand, ICommandContext?, NestedTestCommandResponse>? nestedHandlerFn = null,
                                       MiddlewareFn? middlewareFn = null,
                                       MiddlewareFn? outerMiddlewareFn = null,
                                       Action<ICommandContext?>? nestedClassFn = null,
                                       Action<ICommandContext?>? handlerPreReturnFn = null,
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

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<ICommandContextAccessor>()), nestedClassLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandHandler),
                                                        p => new TestCommandHandler(handlerFn,
                                                                                    handlerPreReturnFn,
                                                                                    p.GetRequiredService<ICommandContextAccessor>(),
                                                                                    p.GetRequiredService<NestedClass>(),
                                                                                    p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()),
                                                        handlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedTestCommandHandler),
                                                        p => new NestedTestCommandHandler(nestedHandlerFn, p.GetRequiredService<ICommandContextAccessor>()),
                                                        nestedHandlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandMiddleware),
                                                        p => new TestCommandMiddleware(middlewareFn, p.GetRequiredService<ICommandContextAccessor>()),
                                                        middlewareLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(OuterTestCommandMiddleware),
                                                        p => new OuterTestCommandMiddleware(outerMiddlewareFn, p.GetRequiredService<ICommandContextAccessor>()),
                                                        middlewareLifetime));

            var provider = services.AddConquerorCQS().FinalizeConquerorRegistrations().BuildServiceProvider();

            _ = provider.GetRequiredService<NestedClass>();
            _ = provider.GetRequiredService<TestCommandHandler>();
            _ = provider.GetRequiredService<TestCommandMiddleware>();
            _ = provider.GetRequiredService<OuterTestCommandMiddleware>();

            return provider.CreateScope().ServiceProvider;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
        private IServiceProvider SetupWithoutResponse(Action<TestCommandWithoutResponse, ICommandContext?>? handlerFn = null,
                                                      MiddlewareFn? middlewareFn = null,
                                                      Action<ICommandContext?>? nestedClassFn = null,
                                                      ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
                                                      ServiceLifetime middlewareLifetime = ServiceLifetime.Transient,
                                                      ServiceLifetime nestedClassLifetime = ServiceLifetime.Transient)
        {
            handlerFn ??= (_, _) => { };
            middlewareFn ??= (middlewareCtx, _, next) => next(middlewareCtx.Command);
            nestedClassFn ??= _ => { };

            var services = new ServiceCollection();

            _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<ICommandContextAccessor>()), nestedClassLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandHandlerWithoutResponse),
                                                        p => new TestCommandHandlerWithoutResponse(handlerFn, p.GetRequiredService<ICommandContextAccessor>(), p.GetRequiredService<NestedClass>()),
                                                        handlerLifetime));

            _ = services.Add(ServiceDescriptor.Describe(typeof(TestCommandMiddleware),
                                                        p => new TestCommandMiddleware(middlewareFn, p.GetRequiredService<ICommandContextAccessor>()),
                                                        middlewareLifetime));

            var provider = services.AddConquerorCQS().FinalizeConquerorRegistrations().BuildServiceProvider();

            _ = provider.GetRequiredService<NestedClass>();
            _ = provider.GetRequiredService<TestCommandHandlerWithoutResponse>();
            _ = provider.GetRequiredService<TestCommandMiddleware>();

            return provider.CreateScope().ServiceProvider;
        }

        private delegate Task<TestCommandResponse> MiddlewareFn(CommandMiddlewareContext<TestCommand, TestCommandResponse> middlewareCtx,
                                                                ICommandContext? ctx,
                                                                Func<TestCommand, Task<TestCommandResponse>> next);

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int Payload);

        private sealed record TestCommandWithoutResponse(int Payload);

        private sealed record NestedTestCommand(int Payload);

        private sealed record NestedTestCommandResponse(int Payload);

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly Func<TestCommand, ICommandContext?, TestCommandResponse> handlerFn;
            private readonly NestedClass nestedClass;
            private readonly ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler;
            private readonly Action<ICommandContext?> preReturnFn;

            public TestCommandHandler(Func<TestCommand, ICommandContext?, TestCommandResponse> handlerFn,
                                      Action<ICommandContext?> preReturnFn,
                                      ICommandContextAccessor commandContextAccessor,
                                      NestedClass nestedClass,
                                      ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler)
            {
                this.handlerFn = handlerFn;
                this.commandContextAccessor = commandContextAccessor;
                this.nestedClass = nestedClass;
                this.nestedCommandHandler = nestedCommandHandler;
                this.preReturnFn = preReturnFn;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                var response = handlerFn(command, commandContextAccessor.CommandContext);
                nestedClass.Execute();
                _ = await nestedCommandHandler.ExecuteCommand(new(command.Payload), cancellationToken);
                preReturnFn(commandContextAccessor.CommandContext);
                return response;
            }

            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline) => pipeline.Use<OuterTestCommandMiddleware>()
                                                                                              .Use<TestCommandMiddleware>();
        }

        private sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly Action<TestCommandWithoutResponse, ICommandContext?> handlerFn;
            private readonly NestedClass nestedClass;

            public TestCommandHandlerWithoutResponse(Action<TestCommandWithoutResponse, ICommandContext?> handlerFn, ICommandContextAccessor commandContextAccessor, NestedClass nestedClass)
            {
                this.handlerFn = handlerFn;
                this.commandContextAccessor = commandContextAccessor;
                this.nestedClass = nestedClass;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                handlerFn(command, commandContextAccessor.CommandContext);
                nestedClass.Execute();
            }
        }

        private sealed class NestedTestCommandHandler : ICommandHandler<NestedTestCommand, NestedTestCommandResponse>
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly Func<NestedTestCommand, ICommandContext?, NestedTestCommandResponse> handlerFn;

            public NestedTestCommandHandler(Func<NestedTestCommand, ICommandContext?, NestedTestCommandResponse> handlerFn, ICommandContextAccessor commandContextAccessor)
            {
                this.handlerFn = handlerFn;
                this.commandContextAccessor = commandContextAccessor;
            }

            public async Task<NestedTestCommandResponse> ExecuteCommand(NestedTestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                return handlerFn(command, commandContextAccessor.CommandContext);
            }
        }

        private sealed class OuterTestCommandMiddleware : ICommandMiddleware
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly MiddlewareFn middlewareFn;

            public OuterTestCommandMiddleware(MiddlewareFn middlewareFn, ICommandContextAccessor commandContextAccessor)
            {
                this.middlewareFn = middlewareFn;
                this.commandContextAccessor = commandContextAccessor;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, commandContextAccessor.CommandContext, async cmd =>
                {
                    var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                    return (response as TestCommandResponse)!;
                });
            }
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly MiddlewareFn middlewareFn;

            public TestCommandMiddleware(MiddlewareFn middlewareFn, ICommandContextAccessor commandContextAccessor)
            {
                this.middlewareFn = middlewareFn;
                this.commandContextAccessor = commandContextAccessor;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, commandContextAccessor.CommandContext, async cmd =>
                {
                    var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                    return (response as TestCommandResponse)!;
                });
            }
        }

        private sealed class NestedClass
        {
            private readonly ICommandContextAccessor commandContextAccessor;
            private readonly Action<ICommandContext?> nestedClassFn;

            public NestedClass(Action<ICommandContext?> nestedClassFn, ICommandContextAccessor commandContextAccessor)
            {
                this.nestedClassFn = nestedClassFn;
                this.commandContextAccessor = commandContextAccessor;
            }

            public void Execute()
            {
                nestedClassFn(commandContextAccessor.CommandContext);
            }
        }
    }
}
