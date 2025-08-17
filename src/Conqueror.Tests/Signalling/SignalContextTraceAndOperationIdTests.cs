namespace Conqueror.Tests.Signalling;

public sealed partial class SignalContextTraceAndOperationIdTests
{
    private static int TestCaseCounter;

    [Test]
    [Combinatorial]
    public async Task GivenSetup_WhenExecutingHandler_OperationIdsAreCorrectlyAvailable(
        [Values(arg1: true, arg2: false)] bool hasCustomTraceId,
        [Values(arg1: true, arg2: false)] bool hasActivity,
        [Values(arg1: true, arg2: false)] bool publishNestedWithDifferentTransport
    )
    {
        var customTraceId = Guid.NewGuid().ToString();

        string? traceIdFromExecution = null;
        string? traceIdFromHandler = null;
        string? messageIdFromHandler = null;
        string? traceIdFromNestedSignalHandler = null;
        string? messageIdFromNestedSignalHandler = null;

        var services = new ServiceCollection();

        _ = services
            .AddSignalHandlerDelegate(
                TestSignal.T,
                async (_, p, ct) =>
                {
                    await Task.Yield();
                    traceIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.TraceId;
                    messageIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.SignalId;

                    var handler = p.GetRequiredService<ISignalPublishers>().For(NestedTestSignal.T);

                    if (publishNestedWithDifferentTransport)
                    {
                        handler = handler.WithTransport(b => new TestSignalPublisher<NestedTestSignal>(
                            b.UseInProcess()
                        ));
                    }

                    await handler.Handle(new(), ct);
                }
            )
            .AddSignalHandlerDelegate(
                NestedTestSignal.T,
                async (_, p, _) =>
                {
                    await Task.Yield();
                    traceIdFromNestedSignalHandler =
                        p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.TraceId;
                    messageIdFromNestedSignalHandler =
                        p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext?.SignalId;
                }
            );

        await using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true }
        );

        ConquerorContext? conquerorContext = null;

        if (hasCustomTraceId)
        {
            conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();
            conquerorContext.TraceId = customTraceId;
        }

        using var d = conquerorContext;

        var testCaseIdx = Interlocked.Increment(ref TestCaseCounter);
        using var activity = hasActivity
            ? StartActivity($"{nameof(SignalContextTraceAndOperationIdTests)}{testCaseIdx}")
            : null;

        var handlerSender = serviceProvider
            .GetRequiredService<ISignalPublishers>()
            .For(TestSignal.T)
            .WithPipeline(p =>
                p.Use(ctx =>
                {
                    traceIdFromExecution = ctx.ConquerorContext.TraceId;

                    return ctx.Next(ctx.Signal, ctx.CancellationToken);
                })
            )
            .WithTransport(b => b.UseInProcess());

        await handlerSender.Handle(new(), CancellationToken.None);

        var expectedTraceId = (hasCustomTraceId, hasActivity) switch
        {
            (true, _) => customTraceId,
            (false, true) => activity!.TraceId,
            (false, false) => traceIdFromExecution,
        };

        Assert.Multiple(() =>
        {
            Assert.That(traceIdFromHandler, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromNestedSignalHandler, Is.EqualTo(expectedTraceId));

            Assert.That(messageIdFromHandler, Is.Not.Null);
            Assert.That(messageIdFromNestedSignalHandler, Is.Not.EqualTo(messageIdFromHandler));
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

        return new DisposableActivity(activity.TraceId.ToString(), activitySource, activityListener, activity);
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

    [Signal]
    private sealed partial record TestSignal;

    [Signal]
    private sealed partial record NestedTestSignal;

    private sealed class TestSignalPublisher<TSignal>(ISignalPublisher<TSignal> wrapped) : ISignalPublisher<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public string TransportTypeName => "test";

        public Task Publish(
            TSignal signal,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken
        ) => wrapped.Publish(signal, serviceProvider, conquerorContext, cancellationToken);
    }
}
