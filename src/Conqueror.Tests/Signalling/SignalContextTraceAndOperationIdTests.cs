using System.Diagnostics;

namespace Conqueror.Tests.Signalling;

public sealed partial class SignalContextTraceAndOperationIdTests
{
    private static int testCaseCounter;

    [Test]
    [Combinatorial]
    [SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "parameter name makes sense here")]
    public async Task GivenSetup_WhenExecutingHandler_OperationIdsAreCorrectlyAvailable(
        [Values(true, false)] bool hasCustomTraceId,
        [Values(true, false)] bool hasActivity)
    {
        var customTraceId = Guid.NewGuid().ToString();

        string? traceIdFromTransportBuilder = null;
        string? messageIdFromTransportBuilder = null;
        string? traceIdFromHandler = null;
        string? messageIdFromHandler = null;
        string? traceIdFromNestedSignalHandler = null;
        string? messageIdFromNestedSignalHandler = null;

        var services = new ServiceCollection();

        _ = services.AddSignalHandlerDelegate(
                        TestSignal.T,
                        async (_, p, ct) =>
                        {
                            await Task.Yield();
                            traceIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                            messageIdFromHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetSignalId();
                            await p.GetRequiredService<ISignalPublishers>().For(NestedTestSignal.T).Handle(new(), ct);
                        })
                    .AddSignalHandlerDelegate(
                        NestedTestSignal.T,
                        async (_, p, _) =>
                        {
                            await Task.Yield();
                            traceIdFromNestedSignalHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetTraceId();
                            messageIdFromNestedSignalHandler = p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!.GetSignalId();
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
        using var activity = hasActivity ? StartActivity(nameof(SignalContextTraceAndOperationIdTests) + testCaseIdx) : null;

        var handlerSender = serviceProvider.GetRequiredService<ISignalPublishers>()
                                           .For(TestSignal.T)
                                           .WithTransport(b =>
                                           {
                                               traceIdFromTransportBuilder = b.ConquerorContext.GetTraceId();
                                               messageIdFromTransportBuilder = b.ConquerorContext.GetSignalId();

                                               return b.UseInProcessWithSequentialBroadcastingStrategy();
                                           });

        await handlerSender.Handle(new());

        var expectedTraceId = (hasCustomTraceId, hasActivity) switch
        {
            (true, _) => customTraceId,
            (false, true) => activity!.TraceId,
            (false, false) => traceIdFromTransportBuilder,
        };

        Assert.Multiple(() =>
        {
            Assert.That(traceIdFromTransportBuilder, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromHandler, Is.EqualTo(expectedTraceId));
            Assert.That(traceIdFromNestedSignalHandler, Is.EqualTo(expectedTraceId));

            Assert.That(messageIdFromTransportBuilder, Is.EqualTo(messageIdFromHandler));
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

        return new(
            activity.TraceId.ToString(),
            activitySource,
            activityListener,
            activity);
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
}
