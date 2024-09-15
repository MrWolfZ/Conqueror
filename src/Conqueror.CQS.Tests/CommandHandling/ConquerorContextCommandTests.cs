// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;

namespace Conqueror.CQS.Tests.CommandHandling;

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

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandlerWithoutResponse()
    {
        var command = new TestCommandWithoutResponse(10);

        var provider = SetupWithoutResponse((_, ctx) => { Assert.That(ctx, Is.Not.Null); });

        await provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>().Handle(command, CancellationToken.None);
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

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsAvailableInNestedClass()
    {
        var command = new TestCommand(10);

        var provider = Setup(
            nestedClassFn: b => Assert.That(b, Is.Not.Null),
            nestedClassLifetime: ServiceLifetime.Scoped);

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
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

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsAvailableInHandlerAfterExecutionOfNestedHandler()
    {
        var command = new TestCommand(10);

        var provider = Setup(handlerPreReturnFn: b => Assert.That(b, Is.Not.Null));

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenCommandExecution_ConquerorContextIsTheSameInMiddlewareHandlerAndNestedClassWithConfigureAwait()
    {
        var command = new TestCommand(10);
        var observedContexts = new List<ConquerorContext>();

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

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);

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
                observedTraceIds.Add(ctx!.GetTraceId());
                return new(cmd.Payload);
            },
            (cmd, _) => new(cmd.Payload),
            (ctx, next) =>
            {
                observedTraceIds.Add(ctx.ConquerorContext.GetTraceId());
                return next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => observedTraceIds.Add(ctx!.GetTraceId()));

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);

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
                observedTraceIds.Add(ctx!.GetTraceId());
                return new(cmd.Payload);
            },
            (cmd, _) => new(cmd.Payload),
            (ctx, next) =>
            {
                observedTraceIds.Add(ctx.ConquerorContext.GetTraceId());
                return next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => observedTraceIds.Add(ctx!.GetTraceId()));

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);

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
                observedTraceIds.Add(ctx!.GetTraceId());
                return new(cmd.Payload);
            },
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.GetTraceId());
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);

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
                observedTraceIds.Add(ctx!.GetTraceId());
                return new(cmd.Payload);
            },
            (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.GetTraceId());
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
        Assert.That(observedTraceIds[0], Is.SameAs(activity.TraceId));
    }

    [Test]
    public void GivenNoCommandExecution_ConquerorContextIsNotAvailable()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

        _ = services.AddTransient(p => new NestedClass(b => Assert.That(b, Is.Null), p.GetRequiredService<IConquerorContextAccessor>()));

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
            Assert.That(ctx?.GetTraceId(), Is.EqualTo(expectedTraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.GetTraceId();

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInHandler));

        var command = new TestCommand(10);

        var provider = Setup((cmd, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.GetTraceId(), Is.EqualTo(activity.TraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContext_TraceIdIsAvailableInNestedHandler()
    {
        var command = new TestCommand(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.GetTraceId(), Is.EqualTo(expectedTraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.GetTraceId();

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedHandler));

        var command = new TestCommand(10);

        var provider = Setup(nestedHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.GetTraceId(), Is.EqualTo(activity.TraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().Handle(command, CancellationToken.None);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestCommand, ConquerorContext?, TestCommandResponse>? handlerFn = null,
                                   Func<NestedTestCommand, ConquerorContext?, NestedTestCommandResponse>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<ConquerorContext?>? nestedClassFn = null,
                                   Action<ConquerorContext?>? handlerPreReturnFn = null,
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
                                                                             p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()));

        _ = services.AddConquerorCommandHandler<NestedTestCommandHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()));

        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), middlewareFn);
        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(OuterTestCommandMiddleware<TestCommand, TestCommandResponse>), outerMiddlewareFn);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandler>();

        return provider.CreateScope().ServiceProvider;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider SetupWithoutResponse(Action<TestCommandWithoutResponse, ConquerorContext?>? handlerFn = null,
                                                  MiddlewareFnWithoutResponse? middlewareFn = null,
                                                  Action<ConquerorContext?>? nestedClassFn = null)
    {
        handlerFn ??= (_, _) => { };
        middlewareFn ??= (ctx, next) => next(ctx.Command);
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>(p => new(handlerFn, p.GetRequiredService<IConquerorContextAccessor>(), p.GetRequiredService<NestedClass>()));

        _ = services.AddKeyedSingleton<MiddlewareFnWithoutResponse>(typeof(TestCommandMiddlewareWithoutResponse<TestCommandWithoutResponse, UnitCommandResponse>), middlewareFn);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandlerWithoutResponse>();

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

    private sealed class DisposableActivity(string traceId, params IDisposable[] disposables) : IDisposable
    {
        private readonly IReadOnlyCollection<IDisposable> disposables = disposables;

        public string TraceId { get; } = traceId;

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

    private delegate Task MiddlewareFnWithoutResponse(CommandMiddlewareContext<TestCommandWithoutResponse, UnitCommandResponse> middlewareCtx,
                                                      Func<TestCommandWithoutResponse, Task> next);

    private sealed record TestCommand(int Payload);

    private sealed record TestCommandResponse(int Payload);

    private sealed record TestCommandWithoutResponse(int Payload);

    private sealed record NestedTestCommand(int Payload);

    private sealed record NestedTestCommandResponse(int Payload);

    private sealed class TestCommandHandler(
        Func<TestCommand, ConquerorContext?, TestCommandResponse> handlerFn,
        Action<ConquerorContext?> preReturnFn,
        IConquerorContextAccessor conquerorContextAccessor,
        NestedClass nestedClass,
        ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler)
        : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(command, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedCommandHandler.Handle(new(command.Payload), cancellationToken);
            preReturnFn(conquerorContextAccessor.ConquerorContext);
            return response;
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestCommandResponse> pipeline) => pipeline.Use(new OuterTestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFn>(typeof(OuterTestCommandMiddleware<TestCommand, TestCommandResponse>))))
                                                                                                                     .Use(new TestCommandMiddleware<TestCommand, TestCommandResponse>(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFn>(typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>))));
    }

    private sealed class TestCommandHandlerWithoutResponse(
        Action<TestCommandWithoutResponse, ConquerorContext?> handlerFn,
        IConquerorContextAccessor conquerorContextAccessor,
        NestedClass nestedClass)
        : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            handlerFn(command, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommandWithoutResponse> pipeline) => pipeline.Use(new TestCommandMiddlewareWithoutResponse<TestCommandWithoutResponse, UnitCommandResponse>(pipeline.ServiceProvider.GetRequiredKeyedService<MiddlewareFnWithoutResponse>(typeof(TestCommandMiddlewareWithoutResponse<TestCommandWithoutResponse, UnitCommandResponse>))));
    }

    private sealed class NestedTestCommandHandler(
        Func<NestedTestCommand, ConquerorContext?, NestedTestCommandResponse> handlerFn,
        IConquerorContextAccessor conquerorContextAccessor)
        : ICommandHandler<NestedTestCommand, NestedTestCommandResponse>
    {
        public async Task<NestedTestCommandResponse> Handle(NestedTestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return handlerFn(command, conquerorContextAccessor.ConquerorContext);
        }
    }

    private sealed class OuterTestCommandMiddleware<TCommand, TResponse>(MiddlewareFn middlewareFn) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                return (response as TestCommandResponse)!;
            });
        }
    }

    private sealed class TestCommandMiddleware<TCommand, TResponse>(MiddlewareFn middlewareFn) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            return (TResponse)(object)await middlewareFn((ctx as CommandMiddlewareContext<TestCommand, TestCommandResponse>)!, async cmd =>
            {
                var response = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
                return (response as TestCommandResponse)!;
            });
        }
    }

    private sealed class TestCommandMiddlewareWithoutResponse<TCommand, TResponse>(MiddlewareFnWithoutResponse middlewareFn) : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();
            await middlewareFn((ctx as CommandMiddlewareContext<TestCommandWithoutResponse, UnitCommandResponse>)!, async cmd =>
            {
                _ = await ctx.Next((cmd as TCommand)!, ctx.CancellationToken);
            });

            return (TResponse)(object)UnitCommandResponse.Instance;
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
