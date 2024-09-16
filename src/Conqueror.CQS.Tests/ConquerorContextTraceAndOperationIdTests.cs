using System.Diagnostics;

namespace Conqueror.CQS.Tests;

public sealed class ConquerorContextTraceAndOperationIdTests
{
    private static int testCaseCounter;

    [Test]
    [Combinatorial]
    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "parameter name makes sense here")]
    public async Task GivenSetup_WhenExecutingHandler_OperationIdsAreCorrectlyAvailable(
        [Values(RequestType.Command, RequestType.Query)] string requestType,
        [Values(true, false)] bool hasCustomTraceId,
        [Values(true, false)] bool hasActivity)
    {
        var customTraceId = Guid.NewGuid().ToString();

        string? traceIdFromClientTransportBuilder = null;
        string? operationIdFromClientTransportBuilder = null;
        string? traceIdFromCommandHandler = null;
        string? commandIdFromCommandHandler = null;
        string? traceIdFromNestedCommandHandler = null;
        string? commandIdFromNestedCommandHandler = null;
        string? queryIdFromNestedCommandHandler = null;
        string? traceIdFromQueryHandler = null;
        string? queryIdFromQueryHandler = null;
        string? traceIdFromNestedQueryHandler = null;
        string? commandIdFromNestedQueryHandler = null;
        string? queryIdFromNestedQueryHandler = null;

        var services = new ServiceCollection();

        _ = services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(async (cmd, p, ct) =>
                    {
                        await Task.Yield();
                        traceIdFromCommandHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                        commandIdFromCommandHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetCommandId();
                        _ = await p.GetRequiredService<ICommandHandler<NestedTestCommand, TestCommandResponse>>().Handle(new(), ct);
                        _ = await p.GetRequiredService<IQueryHandler<NestedTestQuery, TestQueryResponse>>().Handle(new(), ct);
                        return new();
                    })
                    .AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (qry, p, ct) =>
                    {
                        await Task.Yield();
                        traceIdFromQueryHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                        queryIdFromQueryHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetQueryId();
                        _ = await p.GetRequiredService<ICommandHandler<NestedTestCommand, TestCommandResponse>>().Handle(new(), ct);
                        _ = await p.GetRequiredService<IQueryHandler<NestedTestQuery, TestQueryResponse>>().Handle(new(), ct);
                        return new();
                    })
                    .AddConquerorCommandHandlerDelegate<NestedTestCommand, TestCommandResponse>(async (_, p, _) =>
                    {
                        await Task.Yield();
                        traceIdFromNestedCommandHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                        commandIdFromNestedCommandHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetCommandId();
                        queryIdFromNestedCommandHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetQueryId();
                        return new();
                    })
                    .AddConquerorQueryHandlerDelegate<NestedTestQuery, TestQueryResponse>(async (_, p, _) =>
                    {
                        await Task.Yield();
                        traceIdFromNestedQueryHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                        commandIdFromNestedQueryHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetCommandId();
                        queryIdFromNestedQueryHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetQueryId();
                        return new();
                    });

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        ConquerorContext? conquerorContext = null;

        if (hasCustomTraceId)
        {
            conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();
            conquerorContext.SetTraceId(customTraceId);
        }

        using var d = conquerorContext;

        var testCaseIdx = Interlocked.Increment(ref testCaseCounter);
        using var activity = hasActivity ? StartActivity(nameof(ConquerorContextTraceAndOperationIdTests) + testCaseIdx) : null;

        if (requestType == RequestType.Command)
        {
            var handlerClient = serviceProvider.GetRequiredService<ICommandClientFactory>()
                                               .CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b =>
                                               {
                                                   traceIdFromClientTransportBuilder = b.ConquerorContext.GetTraceId();
                                                   operationIdFromClientTransportBuilder = b.ConquerorContext.GetCommandId();
                                                   return b.UseInProcess();
                                               });

            _ = await handlerClient.Handle(new());
        }

        if (requestType == RequestType.Query)
        {
            var handlerClient = serviceProvider.GetRequiredService<IQueryClientFactory>()
                                               .CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b =>
                                               {
                                                   traceIdFromClientTransportBuilder = b.ConquerorContext.GetTraceId();
                                                   operationIdFromClientTransportBuilder = b.ConquerorContext.GetQueryId();
                                                   return b.UseInProcess();
                                               });

            _ = await handlerClient.Handle(new());
        }

        var expectedTraceId = (hasCustomTraceId, hasActivity) switch
        {
            (true, _) => customTraceId,
            (false, true) => activity!.TraceId,
            (false, false) => traceIdFromClientTransportBuilder,
        };

        Assert.Multiple(() =>
        {
            Assert.That(traceIdFromClientTransportBuilder, Is.EqualTo(expectedTraceId));
            Assert.That(requestType == RequestType.Command ? traceIdFromCommandHandler : traceIdFromQueryHandler, Is.EqualTo(expectedTraceId));
            Assert.That(requestType == RequestType.Query ? traceIdFromCommandHandler : traceIdFromQueryHandler, Is.Null);
            Assert.That(traceIdFromNestedCommandHandler, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromNestedQueryHandler, Is.EqualTo(expectedTraceId));

            Assert.That(operationIdFromClientTransportBuilder, Is.EqualTo(requestType == RequestType.Command ? commandIdFromCommandHandler : queryIdFromQueryHandler));
            Assert.That(commandIdFromCommandHandler, requestType == RequestType.Command ? Is.Not.Null : Is.Null);
            Assert.That(commandIdFromNestedCommandHandler, requestType == RequestType.Command ? Is.Not.EqualTo(commandIdFromCommandHandler) : Is.Not.Null);
            Assert.That(queryIdFromNestedCommandHandler, Is.EqualTo(requestType == RequestType.Query ? queryIdFromQueryHandler : null));
            Assert.That(queryIdFromQueryHandler, requestType == RequestType.Query ? Is.Not.Null : Is.Null);
            Assert.That(commandIdFromNestedQueryHandler, Is.EqualTo(requestType == RequestType.Command ? commandIdFromCommandHandler : null));
            Assert.That(queryIdFromNestedQueryHandler, requestType == RequestType.Query ? Is.Not.EqualTo(queryIdFromQueryHandler) : Is.Not.Null);
        });
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

    private static class RequestType
    {
        public const string Command = nameof(Command);
        public const string Query = nameof(Query);
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed record NestedTestCommand;

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    private sealed record NestedTestQuery;
}
