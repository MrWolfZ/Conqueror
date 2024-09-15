namespace Conqueror.CQS.Tests.CommandHandling;

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
    private IServiceProvider Setup(Func<TestCommand, ConquerorContext?, TestCommandResponse>? handlerFn = null,
                                   Func<NestedTestCommand, ConquerorContext?, NestedTestCommandResponse>? nestedHandlerFn = null,
                                   MiddlewareFn? middlewareFn = null,
                                   MiddlewareFn? outerMiddlewareFn = null,
                                   Action<ConquerorContext?>? nestedClassFn = null)
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

        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(TestCommandMiddleware<TestCommand, TestCommandResponse>), middlewareFn);
        _ = services.AddKeyedSingleton<MiddlewareFn>(typeof(OuterTestCommandMiddleware<TestCommand, TestCommandResponse>), outerMiddlewareFn);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<NestedClass>();
        _ = provider.GetRequiredService<TestCommandHandler>();

        return provider.CreateScope().ServiceProvider;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "fine for testing")]
    private IServiceProvider SetupWithoutResponse(Action<TestCommandWithoutResponse, ConquerorContext?>? handlerFn = null,
                                                  Action<ConquerorContext?>? nestedClassFn = null)
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

    private sealed class TestCommandHandler(
        Func<TestCommand, ConquerorContext?, TestCommandResponse> handlerFn,
        IConquerorContextAccessor conquerorContextAccessor,
        NestedClass nestedClass,
        ICommandHandler<NestedTestCommand, NestedTestCommandResponse> nestedCommandHandler)
        : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            var response = handlerFn(command, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
            _ = await nestedCommandHandler.ExecuteCommand(new(command.Payload), cancellationToken);
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
        public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            handlerFn(command, conquerorContextAccessor.ConquerorContext);
            nestedClass.Execute();
        }
    }

    private sealed class NestedTestCommandHandler(
        Func<NestedTestCommand, ConquerorContext?, NestedTestCommandResponse> handlerFn,
        IConquerorContextAccessor conquerorContextAccessor)
        : ICommandHandler<NestedTestCommand, NestedTestCommandResponse>
    {
        public async Task<NestedTestCommandResponse> ExecuteCommand(NestedTestCommand command, CancellationToken cancellationToken = default)
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

    private sealed class NestedClass(Action<ConquerorContext?> nestedClassFn, IConquerorContextAccessor conquerorContextAccessor)
    {
        public void Execute()
        {
            nestedClassFn(conquerorContextAccessor.ConquerorContext);
        }
    }
}
