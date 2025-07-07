using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using static Conqueror.Transport.ConformityTests.TestContextData;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Conqueror.Transport.ConformityTests.Signalling;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "by design")]
public abstract class SignalTransportContextConformityTests<TTestClass, TTestHost, TTestCase>
    where TTestClass : SignalTransportContextConformityTests<TTestClass, TTestHost, TTestCase>,
    ISignalTransportContextConformityTests<TTestHost, TTestCase>
    where TTestHost : ISignalTransportConformityTestHost
    where TTestCase : ISignalTransportConformityContextTestCase<TTestHost>
{
    [Test]
    [TestCaseSource(nameof(CreateTestCasesPrivate))]
    public async Task GivenContextData_WhenPublishingSignal_DataIsCorrectlySent(TTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        var seenSignalIdsOnPublisher = new ConcurrentQueue<string?>();
        var seenTraceIdsOnPublisher = new ConcurrentQueue<string>();

        await using var publisherHost = await host.CreatePublisherTestHost(
            host.TestTimeoutToken,
            (_, ctx, _) =>
            {
                seenSignalIdsOnPublisher.Enqueue(ctx.SignalId);
                seenTraceIdsOnPublisher.Enqueue(ctx.TraceId);

                return Task.CompletedTask;
            });

        var receivedSignalIds = new ConcurrentQueue<string?>();
        var receivedTraceIds = new ConcurrentQueue<string>();

        var receivedDownstreamContextDatas = new ConcurrentQueue<IReadOnlyCollection<(string, string)>>();
        var receivedBidirectionalContextDatas = new ConcurrentQueue<IReadOnlyCollection<(string, string)>>();

        DisposableActivity? activity = null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (_, ctx, _) =>
            {
                receivedSignalIds.Enqueue(ctx.SignalId);
                receivedTraceIds.Enqueue(ctx.TraceId);

                receivedDownstreamContextDatas.Enqueue(ctx.TransportableData.GetAll(flowDirection: ConquerorContextDataFlowDirection.Downstream).ToList());
                receivedBidirectionalContextDatas.Enqueue(ctx.TransportableData.GetAll(ConquerorContextDataFlowDirection.Bidirectional).ToList());

                return Task.CompletedTask;
            });

        _ = receiverHost.ReceiverExecutionHandle?.CompletionTask.ContinueWith(
            (t, logger) =>
            {
                ((ILogger)logger!).LogError(t.Exception!, "error in run");
            },
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        await Assert.ThatAsync(
            () => receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken) ?? Task.CompletedTask,
            Throws.Nothing);

        await testCase.BeforePublish(host);

        if (testCase.HasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenPublishingSignal_DataIsCorrectlySent));
            _ = activity.Activity.Start();
        }

        using var conquerorContext = publisherHost.ConquerorContextAccessor.GetOrCreate();

        using var d = activity;

        if (testCase.HasDownstreamData)
        {
            foreach (var (key, value) in ContextDataDownstreamAcrossTransports)
            {
                conquerorContext.TransportableData.Set(key, value, flowDirection: ConquerorContextDataFlowDirection.Downstream);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.InProcessData.Set(key, value, flowDirection: ConquerorContextDataFlowDirection.Downstream);
            }
        }

        if (testCase.HasBidirectionalData)
        {
            foreach (var (key, value) in ContextDataDownstreamBidirectionalAcrossTransports)
            {
                conquerorContext.TransportableData.Set(key, value, ConquerorContextDataFlowDirection.Bidirectional);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.InProcessData.Set(key, value, ConquerorContextDataFlowDirection.Bidirectional);
            }
        }

        await testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken);

        Assert.That(
            () => receivedSignalIds,
            Is.EquivalentTo(Enumerable.Repeat(seenSignalIdsOnPublisher, testCase.NumOfReceivers).SelectMany(e => e))
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        Assert.That(receivedTraceIds, Is.EquivalentTo(Enumerable.Repeat(seenTraceIdsOnPublisher, testCase.NumOfReceivers).SelectMany(e => e)));

        if (activity is not null)
        {
            Assert.That(receivedTraceIds, Is.EqualTo(Enumerable.Repeat(activity.TraceId, seenTraceIdsOnPublisher.Count * testCase.NumOfReceivers)));
        }

        Assert.That(receivedDownstreamContextDatas, Has.Count.EqualTo(seenTraceIdsOnPublisher.Count * testCase.NumOfReceivers));
        foreach (var receivedDownstreamContextData in receivedDownstreamContextDatas)
        {
            if (testCase.HasDownstreamData)
            {
                Assert.That(receivedDownstreamContextData, Is.SupersetOf(ContextDataDownstreamAcrossTransports.Select(p => (p.Key, p.Value))));
            }
            else
            {
                Assert.That(receivedDownstreamContextData.Intersect(ContextDataDownstreamAcrossTransports.Select(p => (p.Key, p.Value))), Is.Empty);
            }
        }

        Assert.That(receivedBidirectionalContextDatas, Has.Count.EqualTo(seenTraceIdsOnPublisher.Count * testCase.NumOfReceivers));
        foreach (var receivedBidirectionalContextData in receivedBidirectionalContextDatas)
        {
            if (testCase.HasBidirectionalData)
            {
                Assert.That(receivedBidirectionalContextData, Is.EquivalentTo(ContextDataDownstreamBidirectionalAcrossTransports.Select(p => (p.Key, p.Value))));
            }
            else
            {
                Assert.That(receivedBidirectionalContextData, Is.Empty);
            }
        }
    }

    [Test]
    public void GivenTransport_WhenGettingTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateTestCases().ToList();

        List<Expression<Func<ISignalTransportConformityContextTestCase<TTestHost>, bool>>> predicates =
        [
            testCase => testCase.NumOfReceivers == 1,
            testCase => testCase.NumOfReceivers > 1,
            testCase => testCase.HasActivity,
            testCase => !testCase.HasActivity,
            testCase => testCase.HasDownstreamData,
            testCase => !testCase.HasDownstreamData,
            testCase => testCase.HasBidirectionalData,
            testCase => !testCase.HasBidirectionalData,
        ];

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityContextTestCase<TTestHost>>(tc => predicate.Compile().Invoke(tc)),
                    $"missing expected test case: {predicate.Body}");
            }
        });
    }

    private static IEnumerable<TestCaseData> CreateTestCasesPrivate()
        => TTestClass.CreateTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));
}
