namespace Conqueror.CQS.Tests;

public sealed class CommandIdTests
{
    [Test]
    public async Task GivenCommandExecution_CommandIdIsTheSameInHandlerAndMiddlewareAndNestedClass()
    {
        var command = new TestCommand(10);
        var observedCommandIds = new List<string?>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedCommandIds.Add(ctx!.GetCommandId());
                return new(cmd.Payload);
            },
            (cmd, _) => new(cmd.Payload),
            (ctx, next) =>
            {
                observedCommandIds.Add(ctx.ConquerorContext.GetCommandId());
                return next(ctx.Command);
            },
            (ctx, next) => next(ctx.Command),
            ctx => { observedCommandIds.Add(ctx!.GetCommandId()); });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedCommandIds, Has.Count.EqualTo(3));
        Assert.That(observedCommandIds[1], Is.SameAs(observedCommandIds[0]));
        Assert.That(observedCommandIds[2], Is.SameAs(observedCommandIds[0]));
    }

    [Test]
    public async Task GivenCommandExecutionWithoutResponse_CommandIdIsTheSameInHandlerAndNestedClass()
    {
        var command = new TestCommandWithoutResponse(10);
        var observedCommandIds = new List<string?>();

        var provider = SetupWithoutResponse(
            (_, ctx) => observedCommandIds.Add(ctx!.GetCommandId()),
            ctx => observedCommandIds.Add(ctx!.GetCommandId()));

        await provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedCommandIds, Has.Count.EqualTo(2));
        Assert.That(observedCommandIds[1], Is.SameAs(observedCommandIds[0]));
    }

    [Test]
    public async Task GivenCommandExecution_CommandIdIsNotTheSameInNestedHandler()
    {
        var command = new TestCommand(10);
        var observedCommandIds = new List<string?>();

        var provider = Setup(
            (cmd, ctx) =>
            {
                observedCommandIds.Add(ctx!.GetCommandId());
                return new(cmd.Payload);
            },
            (cmd, ctx) =>
            {
                observedCommandIds.Add(ctx!.GetCommandId());
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedCommandIds, Has.Count.EqualTo(2));
        Assert.That(observedCommandIds[1], Is.Not.SameAs(observedCommandIds[0]));
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider Setup(Func<TestCommand, IConquerorContext?, TestCommandResponse>? handlerFn = null,
                                   Func<NestedTestCommand, IConquerorContext?, NestedTestCommandResponse>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<IConquerorContext?>? nestedClassFn = null)
    {
        handlerFn ??= (cmd, _) => new(cmd.Payload);
        nestedHandlerFn ??= (cmd, _) => new(cmd.Payload);
        middlewareFn ??= (ctx, next) => next(ctx.Command);
        outerMiddlewareFn ??= (ctx, next) => next(ctx.Command);
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorCommandHandler<TestCommandHandler>(p => new(handlerFn,
                                                                             p.GetRequiredService<IConquerorContextAccessor>(),
                                                                             p.GetRequiredService<NestedClass>(),
                                                                             p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()));

        _ = services.AddConquerorCommandHandler<NestedTestCommandHandler>(p => new(nestedHandlerFn, p.GetRequiredService<IConquerorContextAccessor>()));

        _ = services.AddConquerorCommandMiddleware<TestCommandMiddleware>(_ => new(middlewareFn));

        _ = services.AddConquerorCommandMiddleware<OuterTestCommandMiddleware>(_ => new(outerMiddlewareFn));

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandler>();
        _ = provider.GetRequiredService<TestCommandMiddleware>();
        _ = provider.GetRequiredService<OuterTestCommandMiddleware>();

        return provider.CreateScope().ServiceProvider;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider SetupWithoutResponse(Action<TestCommandWithoutResponse, IConquerorContext?>? handlerFn = null,
                                                  Action<IConquerorContext?>? nestedClassFn = null)
    {
        handlerFn ??= (_, _) => { };
        nestedClassFn ??= _ => { };

        var services = new ServiceCollection();

        _ = services.Add(ServiceDescriptor.Describe(typeof(NestedClass), p => new NestedClass(nestedClassFn, p.GetRequiredService<IConquerorContextAccessor>()), ServiceLifetime.Transient));

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>(p => new(handlerFn, p.GetRequiredService<IConquerorContextAccessor>(), p.GetRequiredService<NestedClass>()));

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandlerWithoutResponse>();

        return provider.CreateScope().ServiceProvider;
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

        public TestCommandHandler(Func<TestCommand, IConquerorContext?, TestCommandResponse> handlerFn,
                                  IConquerorContextAccessor conquerorContextAccessor,
                                  NestedClass nestedClass,
                                  ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler)
        {
            this.handlerFn = handlerFn;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.nestedClass = nestedClass;
            this.nestedCommandHandler = nestedCommandHandler;
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(command, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedCommandHandler.ExecuteCommand(new(command.Payload), cancellationToken);
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
