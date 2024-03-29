// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

using System.Diagnostics;

namespace Conqueror.CQS.Tests;

public sealed class ConquerorContextComplexTests
{
    [Test]
    public async Task GivenQueryExecution_ConquerorContextIsAvailableInNestedCommandHandler()
    {
        var query = new TestQuery(10);

        var provider = Setup(nestedCommandHandlerFn: (cmd, ctx) =>
        {
            Assert.That(ctx, Is.Not.Null);

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
            Assert.That(ctx, Is.Not.Null);

            return new(query.Payload);
        });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
    }

    [Test]
    public async Task GivenQueryExecution_TraceIdIsTheSameInNestedCommandHandler()
    {
        var query = new TestQuery(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            queryHandlerFn: (_, ctx, next) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return next();
            },
            nestedCommandHandlerFn: (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedCommandHandler()
    {
        using var activity = StartActivity(nameof(GivenQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedCommandHandler));

        var query = new TestQuery(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            queryHandlerFn: (_, ctx, next) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return next();
            },
            nestedCommandHandlerFn: (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenCommandExecution_TraceIdIsTheSameInNestedQueryHandler()
    {
        var command = new TestCommand(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (_, ctx, next) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return next();
            },
            nestedQueryHandlerFn: (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedQueryHandler()
    {
        using var activity = StartActivity(nameof(GivenCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsTheSameInNestedQueryHandler));

        var command = new TestCommand(10);
        var observedTraceIds = new List<string>();

        var provider = Setup(
            (_, ctx, next) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return next();
            },
            nestedQueryHandlerFn: (cmd, ctx) =>
            {
                observedTraceIds.Add(ctx!.TraceId);
                return new(cmd.Payload);
            });

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);

        Assert.That(observedTraceIds, Has.Count.EqualTo(2));
        Assert.That(observedTraceIds[1], Is.SameAs(observedTraceIds[0]));
    }

    [Test]
    public async Task GivenManuallyCreatedContextForQueryExecution_TraceIdIsAvailableInNestedCommandHandler()
    {
        var query = new TestQuery(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedCommandHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToModifiedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(expectedTraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        expectedTraceId = conquerorContext.TraceId;

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextForQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedCommandHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextForQueryExecutionWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedCommandHandler));

        var query = new TestQuery(10);

        var provider = Setup(nestedCommandHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>().ExecuteQuery(query, CancellationToken.None);
    }

    [Test]
    public async Task GivenManuallyCreatedContextForCommandExecution_TraceIdIsAvailableInNestedQueryHandler()
    {
        var command = new TestCommand(10);
        var expectedTraceId = string.Empty;

        var provider = Setup(nestedQueryHandlerFn: (cmd, ctx) =>
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
    public async Task GivenManuallyCreatedContextForCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedQueryHandler()
    {
        using var activity = StartActivity(nameof(GivenManuallyCreatedContextForCommandExecutionWithActiveActivity_TraceIdIsFromActivityAndIsAvailableInNestedQueryHandler));

        var command = new TestCommand(10);

        var provider = Setup(nestedQueryHandlerFn: (cmd, ctx) =>
        {
            // ReSharper disable once AccessToDisposedClosure
            Assert.That(ctx?.TraceId, Is.EqualTo(activity.TraceId));
            return new(cmd.Payload);
        });

        using var conquerorContext = provider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        _ = await provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>().ExecuteCommand(command, CancellationToken.None);
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

        _ = services.AddConquerorCommandHandler<TestCommandHandler>(p => new(commandHandlerFn,
                                                                             p.GetRequiredService<IConquerorContextAccessor>(),
                                                                             p.GetRequiredService<IQueryHandler<NestedTestQuery, NestedTestQueryResponse>>()),
                                                                    commandHandlerLifetime);

        _ = services.AddConquerorCommandHandler<NestedTestCommandHandler>(p => new(nestedCommandHandlerFn,
                                                                                   p.GetRequiredService<IConquerorContextAccessor>()),
                                                                          nestedCommandHandlerLifetime);

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(p => new(queryHandlerFn,
                                                                         p.GetRequiredService<IConquerorContextAccessor>(),
                                                                         p.GetRequiredService<ICommandHandler<NestedTestCommand, NestedTestCommandResponse>>()),
                                                                queryHandlerLifetime);

        _ = services.AddConquerorQueryHandler<NestedTestQueryHandler>(p => new(nestedQueryHandlerFn,
                                                                               p.GetRequiredService<IConquerorContextAccessor>()),
                                                                      nestedQueryHandlerLifetime);

        var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<TestCommandHandler>();
        _ = provider.GetRequiredService<NestedTestCommandHandler>();
        _ = provider.GetRequiredService<TestQueryHandler>();
        _ = provider.GetRequiredService<NestedTestQueryHandler>();

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

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand query, CancellationToken cancellationToken = default)
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

        public async Task<NestedTestCommandResponse> ExecuteCommand(NestedTestCommand query, CancellationToken cancellationToken = default)
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
