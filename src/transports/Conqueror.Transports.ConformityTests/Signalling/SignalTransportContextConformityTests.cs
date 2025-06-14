using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Conqueror.Transports.ConformityTests.Signalling;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "by design")]
public abstract class SignalTransportContextConformityTests<TTestClass, TTestHost, TTestCase>
    where TTestClass : SignalTransportContextConformityTests<TTestClass, TTestHost, TTestCase>,
    ISignalTransportContextConformityTests<TTestHost, TTestCase>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
    where TTestCase : ISignalTransportConformityContextTestCase<TTestHost>
{
    private static readonly Dictionary<string, string> ContextData = new()
    {
        { "key1", "value1" },
        { "key2", "value2" },
        { "keyWith,Comma", "value" },
        { "key4", "valueWith,Comma" },
        { "keyWith=Equals", "value" },
        { "key6", "valueWith=Equals" },
        { "keyWith|Pipe", "value" },
        { "key8", "valueWith|Pipe" },
        { "keyWith:Colon", "value" },
        { "key10", "valueWith:Colon" },
    };

    private static readonly Dictionary<string, string> InProcessContextData = new()
    {
        { "key11", "value1" },
        { "key12", "value2" },
    };

    [Test]
    [TestCaseSource(nameof(CreateTestCasesPrivate))]
    public async Task GivenContextData_WhenPublishingHttpWebSocketsSignal_DataIsCorrectlySent(TTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignalIds = new ConcurrentQueue<string?>();
        var receivedTraceIds = new ConcurrentQueue<string>();

        var receivedDownstreamContextDatas = new ConcurrentQueue<IConquerorContextData>();
        var receivedBidirectionalContextDatas = new ConcurrentQueue<IConquerorContextData>();

        DisposableActivity? activity = null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var handle = testCase.RunReceivers(host.SignalReceivers, cts.Token, signalCallback: (_, ctx, _) =>
        {
            receivedSignalIds.Enqueue(ctx.GetSignalId());
            receivedTraceIds.Enqueue(ctx.GetTraceId());

            receivedDownstreamContextDatas.Enqueue(ctx.DownstreamContextData);
            receivedBidirectionalContextDatas.Enqueue(ctx.ContextData);

            return Task.CompletedTask;
        });

        _ = handle.CompletionTask.ContinueWith(
            (t, logger) =>
            {
                ((ILogger)logger!).LogError(t.Exception!, "error in run");
            },
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        await Assert.ThatAsync(
            () => handle.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 1);

        if (testCase.HasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenPublishingHttpWebSocketsSignal_DataIsCorrectlySent));
            _ = activity.Activity.Start();
        }

        using var conquerorContext = host.PublisherConquerorContextAccessor.GetOrCreate();

        using var d = activity;

        if (testCase.HasDownstreamData)
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

        if (testCase.HasBidirectionalData)
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

        var seenSignalIdsOnPublisher = new ConcurrentQueue<string?>();
        var seenTraceIdsOnPublisher = new ConcurrentQueue<string>();

        await testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken,
                                      (_, ctx, _) =>
                                      {
                                          seenSignalIdsOnPublisher.Enqueue(ctx.GetSignalId());
                                          seenTraceIdsOnPublisher.Enqueue(ctx.GetTraceId());

                                          return Task.CompletedTask;
                                      });

        Assert.That(() => receivedSignalIds,
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
        foreach (var receivedContextData in receivedDownstreamContextDatas)
        {
            if (testCase.HasDownstreamData)
            {
                Assert.That(ContextData, Is.SubsetOf(receivedContextData.AsKeyValuePairs<string>()));
            }
            else
            {
                Assert.That(receivedContextData.WhereScopeIsAcrossTransports().Intersect(ContextData), Is.Empty);
            }
        }

        Assert.That(receivedBidirectionalContextDatas, Has.Count.EqualTo(seenTraceIdsOnPublisher.Count * testCase.NumOfReceivers));
        foreach (var receivedBidirectionalContextData in receivedBidirectionalContextDatas)
        {
            if (testCase.HasBidirectionalData)
            {
                Assert.That(ContextData, Is.SubsetOf(receivedBidirectionalContextData.AsKeyValuePairs<string>()));
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
