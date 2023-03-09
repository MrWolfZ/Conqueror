// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;

namespace Conqueror.CQS.Tests;

public sealed class ConquerorContextCommandTests
{
    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandler()
    {
        var command = new TestCommand(10);

        var provider = Setup((cmd, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

            return new(cmd.Payload);
        });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandlerWithoutResponse()
    {
        var command = new TestCommandWithoutResponse(10);

        var provider = SetupWithoutResponse((_, ctx) => { Assert.That(ctx, Is.Not.Null); });

        await provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>().ExecuteCommand(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsAvailableInMiddleware()
    {
        var command = new TestCommand(10);
        var response = new TestCommandResponse(11);

        var provider = Setup((_, _) => response, middlewareFn: async (ctx, next) =>
        {
            Assert.That(ctx.ConquerorContext, Is.Not.Null);

            return await next(ctx.Command);
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
            Assert.That(ctx, Is.Not.Null);

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
            (ctx, next) =>
            {
                observedContexts.Add(ctx.ConquerorContext);
                return next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => observedContexts.Add(ctx!),
            _ => { },
            handlerLifetime,
            middlewareLifetime,
            nestedClassLifetime);

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
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
            async (ctx, next) =>
            {
                await Task.Delay(10).ConfigureAwait(false);
                observedContexts.Add(ctx.ConquerorContext);
                return await next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => observedContexts.Add(ctx!));

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedContexts, Has.Count.EqualTo(3));
        Assert.That(observedContexts[0], Is.Not.Null);
        Assert.That(observedContexts[1], Is.SameAs(observedContexts[0]));
        Assert.That(observedContexts[2], Is.SameAs(observedContexts[0]));
    }

    [Test]
    public async Task GivenCommandExecution_TraceIdIsTheSameInHandlerMiddlewareAndNestedClass()
    {
        var command = new TestCommand(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            },
            (cmd, _) => new(cmd.Payload),
            (ctx, next) =>
            {
                observedTraceIds.Add(ctx.ConquerorContext.TraceId);
                return next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInHandlerMiddlewareAndNestedClass()
    {
        using var activity = StartActivity(nameof(GivenCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInHandlerMiddlewareAndNestedClass));

        var command = new TestCommand(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            },
            (cmd, _) => new(cmd.Payload),
            (ctx, next) =>
            {
                observedTraceIds.Add(ctx.ConquerorContext.TraceId);
                return next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => observedTraceIds.Add(ctx!.TraceId));

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(3));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[2], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public async Task GivenCommandExecution_TraceIdIsTheSameInNestedHandler()
    {
        var command = new TestCommand(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            },
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedHandler));

        var command = new TestCommand(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            },
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public void GivenNoCommandExecution_ConquerorContextIsNotAvailable()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

        _ = services.AddTransient(p => new NestedClass(Assert.IsNull, p.GetRequiredService<IConquerorContextAccessor>()));

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<NestedClass>().Execute();
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInHandler()
    {
        var command = new TestCommand(10);
        var expectedTraceId = string.Empty;

        var provider = Setup((cmd, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler));

        var command = new TestCommand(10);

        var provider = Setup((cmd, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInNestedHandler()
    {
        var command = new TestCommand(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler));

        var command = new TestCommand(10);

        var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

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
        middlewareFn ??= (ctx, next) => next(ctx.Command);
        outerMiddlewareFn ??= (ctx, next) => next(ctx.Command);
        nestedClassFn ??= _ => { };
        handlerPreReturnFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedClassLifetime));

        _ = services.AddConquerorCommandHandler<TestCommandHandler>(p => new(handlerFn,
                                                                             handlerPreReturnFn,
                                                                             p.GetRequiredService<IConquerorContextAccessor>(),
                                                                             p.GetRequiredService<NestedClass>(),
                                                                             p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()),
                                                                    handlerLifetime);

        _ = services.AddConquerorCommandHandler<NestedTestCommandHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()), nestedHandlerLifetime);

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(_ => new(middlewareFn), middlewareLifetime);

        _ = services.AddConquerorCommandMiddleware<OuterTestCommandMiddleware>(_ => new(outerMiddlewareFn), middlewareLifetime);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandler>();
        _ = provider.GetRequiredService<TestCommandMiddleware>();
        _ = provider.GetRequiredService<OuterTestCommandMiddleware>();

        return provider.CreateScope().ServiceProvider;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider SetupWithoutResponse(Action<TestCommandWithoutResponse, IConquerorContext?>? handlerFn = null,
                                                  MiddlewareFn? middlewareFn = null,
                                                  Action<IConquerorContext?>? nestedClassFn = null)
    {
        handlerFn ??= (_, _) => { };
        middlewareFn ??= (ctx, next) => next(ctx.Command);
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>(p => new(handlerFn, p.GetRequiredService<IConquerorContextAccessor>(), p.GetRequiredService<NestedClass>()));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(_ => new(middlewareFn));

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandlerWithoutResponse>();
        _ = provider.GetRequiredService<TestCommandMiddleware>();

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

    private delegate Task<TestCommandResponse> MiddlewareFn(CommandMiddlewareContext<TestCommand, TestCommandResponse> middlewareCtx,
                                                            Func<TestCommand, Task<TestCommandResponse>> next);

    private sealed record TestCommand(int Payload);

    private sealed record TestCommandResponse(int Payload);

    private sealed record TestCommandWithoutResponse(int Payload);

    private sealed record NestedTestCommand(int Payload);

    private sealed record NestedTestCommandResponse(int Payload);

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, IConfigureCommandPipeline
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

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(command, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedCommandHandler.ExecuteCommand(new(command.Payload), cancellationToken);
            preReturnFn(conquerorContextAccessor.ConquerorContext);
            return response;
        }

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

        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
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

        public async Task<NestedTestCommandResponse> ExecuteCommand(NestedTestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return handlerFn(command, conquerorContextAccessor.ConquerorContext);
        }
    }

    private sealed class OuterTestCommandMiddleware : ICommandMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public OuterTestCommandMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                return (response as TestCommandResponse)!;
            });
        }
    }

    private sealed class TestCommandMiddleware : ICommandMiddleware
    {
        private readonly MiddlewareFn middlewareFn;

        public TestCommandMiddleware(MiddlewareFn middlewareFn)
        {
            this.middlewareFn = middlewareFn;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
            where TCommand : class
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, async cmd =>
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
