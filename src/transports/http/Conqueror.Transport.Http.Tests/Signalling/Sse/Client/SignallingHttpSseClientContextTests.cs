using Microsoft.AspNetCore.Builder;
using static Conqueror.Transport.Http.Tests.HttpTestContextData;
using static Conqueror.Transport.Http.Tests.Signalling.HttpTestSignals;

namespace Conqueror.Transport.Http.Tests.Signalling.Sse.Client;

[TestFixture]
public sealed partial class SignallingHttpSseClientContextTests
{
    private int serverResponseHasBegunCount;

    [Test]
    [TestCaseSource(nameof(GenerateContextDataTestCases))]
    public async Task GivenContextData_WhenPublishingHttpSseSignal_DataIsCorrectlySent(
        bool hasDownstream,
        bool hasBidirectional,
        bool hasActivity)
    {
        await using var host = await HttpTransportTestHost.Create(
            services => services.AddConqueror().AddRouting(),
            app =>
            {
                _ = app.Use(async (ctx, next) =>
                {
                    ctx.Response.OnStarting(() =>
                    {
                        _ = Interlocked.Increment(ref serverResponseHasBegunCount);

                        return Task.CompletedTask;
                    });

                    await next();
                });

                app.MapSignalEndpoints();
            });

        var httpClient = host.HttpClient;

        DisposableActivity? activity = null;

        var callCount = 0;
        List<IConquerorContextData?> receivedContextDatas = [];
        List<IConquerorContextData?> receivedBidirectionalContextDatas = [];
        var clientTestObservations = new TestObservations();

        var clientServices = new ServiceCollection().AddSignalHandler<TestSignalHandler>()
                                                    .AddSignalHandler<NestedTestSignalHandler>()
                                                    .AddSingleton(clientTestObservations)
                                                    .AddSingleton<FnToCallFromHandler>(async (s, p) =>
                                                    {
                                                        var conquerorContextAccessor = p.GetRequiredService<IConquerorContextAccessor>();

                                                        clientTestObservations.ReceivedSignalIds.Enqueue(conquerorContextAccessor.ConquerorContext?.GetSignalId());
                                                        clientTestObservations.ReceivedTraceIds.Enqueue(conquerorContextAccessor.ConquerorContext?.GetTraceId());
                                                        receivedContextDatas.Add(conquerorContextAccessor.ConquerorContext?.DownstreamContextData);
                                                        receivedBidirectionalContextDatas.Add(conquerorContextAccessor.ConquerorContext?.ContextData);

                                                        await p.GetRequiredService<ISignalPublishers>()
                                                               .For(NestedTestSignal.T)
                                                               .Handle(new() { Payload = ((TestSignal)s).Payload });

                                                        callCount += 1;
                                                    })
                                                    .AddSingleton<Action<IHttpSseSignalReceiver>>(r => r.Enable(SseAddress)
                                                                                                        .WithHttpClient(httpClient));

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        await using var run = signalReceivers.RunHttpSseSignalReceivers(host.TestTimeoutToken);

        Assert.That(
            () => serverResponseHasBegunCount,
            Is.EqualTo(1)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        if (hasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenPublishingHttpSseSignal_DataIsCorrectlySent));
            _ = activity.Activity.Start();
        }

        using var conquerorContext = host.Resolve<IConquerorContextAccessor>().GetOrCreate();

        using var d = activity;

        if (hasDownstream)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.InProcess);
            }
        }

        if (hasBidirectional)
        {
            foreach (var (key, value) in ContextData)
            {
                conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.InProcess);
            }
        }

        List<string?> seenSignalIdsOnPublisher = [];
        List<string?> seenTraceIdsOnPublisher = [];

        var handler = host.Resolve<ISignalPublishers>()
                          .For(TestSignal.T)
                          .WithTransport(b =>
                          {
                              seenSignalIdsOnPublisher.Add(b.ConquerorContext.GetSignalId());
                              seenTraceIdsOnPublisher.Add(b.ConquerorContext.GetTraceId());

                              return b.UseHttpServerSentEvents();
                          });

        await handler.Handle(new() { Payload = 10 }, host.TestTimeoutToken);
        await handler.Handle(new() { Payload = 20 }, host.TestTimeoutToken);

        Assert.That(
            () => callCount,
            Is.EqualTo(2)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        Assert.That(clientTestObservations.ReceivedSignalIds, Is.EqualTo(seenSignalIdsOnPublisher));

        // twice to account for nested signal handler
        Assert.That(clientTestObservations.ReceivedTraceIds, Is.EqualTo(seenTraceIdsOnPublisher.Concat(seenTraceIdsOnPublisher)));

        if (activity is not null)
        {
            Assert.That(clientTestObservations.ReceivedTraceIds, Is.EqualTo(Enumerable.Repeat(activity.TraceId, seenTraceIdsOnPublisher.Count * 2)));
        }

        Assert.That(receivedContextDatas.OfType<IConquerorContextData>(), Is.Not.Empty); // always non-empty due to trace ID, etc.
        foreach (var receivedContextData in receivedContextDatas.OfType<IConquerorContextData>())
        {
            if (hasDownstream)
            {
                Assert.That(ContextData, Is.SubsetOf(receivedContextData.AsKeyValuePairs<string>()));
            }
            else
            {
                Assert.That(receivedContextData.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
            }
        }

        Assert.That(receivedContextDatas.OfType<IConquerorContextData>(), Is.Not.Empty);
        foreach (var receivedBidirectionalContextData in receivedBidirectionalContextDatas)
        {
            if (hasBidirectional)
            {
                Assert.That(ContextData, Is.SubsetOf(receivedBidirectionalContextData?.AsKeyValuePairs<string>() ?? []));
            }
            else
            {
                Assert.That(receivedBidirectionalContextData, Is.Empty);
            }
        }
    }

    private static IEnumerable<TestCaseData> GenerateContextDataTestCases()
    {
        return from hasDownstream in new[] { true, false }
               from hasBidirectional in new[] { true, false }
               from hasActivity in new[] { true, false }
               select new TestCaseData(hasDownstream, hasBidirectional, hasActivity);
    }

    [Signal]
    public sealed partial record NestedTestSignal
    {
        public int Payload { get; init; }
    }

    public sealed partial class NestedTestSignalHandler(
        TestObservations testObservations,
        IConquerorContextAccessor contextAccessor)
        : NestedTestSignal.IHandler
    {
        public async Task Handle(NestedTestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            testObservations.ReceivedTraceIds.Enqueue(contextAccessor.ConquerorContext?.GetTraceId());
        }
    }
}
