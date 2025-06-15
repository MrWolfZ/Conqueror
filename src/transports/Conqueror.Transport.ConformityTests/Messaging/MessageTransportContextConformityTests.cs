using System.Collections.Concurrent;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using static Conqueror.Transport.ConformityTests.TestContextData;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Conqueror.Transport.ConformityTests.Messaging;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "by design")]
public abstract class MessageTransportContextConformityTests<TTestClass, TTestHost, TTestCase>
    where TTestClass : MessageTransportContextConformityTests<TTestClass, TTestHost, TTestCase>,
    IMessageTransportContextConformityTests<TTestHost, TTestCase>
    where TTestHost : IMessageTransportConformityTestHost
    where TTestCase : IMessageTransportConformityContextTestCase<TTestHost>
{
    [Test]
    [TestCaseSource(nameof(CreateTestCasesPrivate))]
    public async Task GivenContextData_WhenPublishingHttpWebSocketsMessage_DataIsCorrectlySent(TTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        var receivedMessageIds = new ConcurrentQueue<string?>();
        var receivedTraceIds = new ConcurrentQueue<string>();

        var receivedDownstreamContextDatas = new ConcurrentQueue<IReadOnlyCollection<KeyValuePair<string, string>>>();
        var receivedBidirectionalContextDatas = new ConcurrentQueue<IReadOnlyCollection<KeyValuePair<string, string>>>();

        await using var receiverHost = await host.CreateReceiverTestHost(
            host.TestTimeoutToken,
            (_, ctx, _) =>
            {
                receivedMessageIds.Enqueue(ctx.GetMessageId());
                receivedTraceIds.Enqueue(ctx.GetTraceId());

                receivedDownstreamContextDatas.Enqueue(ctx.DownstreamContextData.AsKeyValuePairs());
                receivedBidirectionalContextDatas.Enqueue(ctx.ContextData.AsKeyValuePairs());

                if (testCase.HasUpstreamData)
                {
                    foreach (var item in ContextDataUpstreamAcrossTransports)
                    {
                        ctx?.UpstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
                    }

                    foreach (var item in InProcessContextData)
                    {
                        ctx?.UpstreamContextData.Set(item.Key, item.Value, ConquerorContextDataScope.InProcess);
                    }
                }

                if (testCase.HasBidirectionalData)
                {
                    foreach (var item in ContextDataUpstreamBidirectionalAcrossTransports)
                    {
                        ctx?.ContextData.Set(item.Key, item.Value, ConquerorContextDataScope.AcrossTransports);
                    }
                }

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

        var seenMessageIdsOnSender = new ConcurrentQueue<string?>();
        var seenTraceIdsOnSender = new ConcurrentQueue<string>();

        DisposableActivity? activity = null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var senderHost = await host.CreateSenderTestHost(
            cts.Token,
            (_, ctx, _) =>
            {
                seenMessageIdsOnSender.Enqueue(ctx.GetMessageId());
                seenTraceIdsOnSender.Enqueue(ctx.GetTraceId());

                return Task.CompletedTask;
            });

        await Assert.ThatAsync(
            () => receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken) ?? Task.CompletedTask,
            Throws.Nothing);

        await testCase.BeforePublish(host);

        if (testCase.HasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenPublishingHttpWebSocketsMessage_DataIsCorrectlySent));
            _ = activity.Activity.Start();
        }

        using var conquerorContext = senderHost.ConquerorContextAccessor.GetOrCreate();

        using var d = activity;

        if (testCase.HasDownstreamData)
        {
            foreach (var (key, value) in ContextDataDownstreamAcrossTransports)
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
            foreach (var (key, value) in ContextDataDownstreamBidirectionalAcrossTransports)
            {
                conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.InProcess);
            }
        }

        _ = await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken);

        Assert.That(
            () => receivedMessageIds,
            Is.EqualTo(seenMessageIdsOnSender)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        Assert.That(receivedTraceIds, Is.EqualTo(seenTraceIdsOnSender));

        if (activity is not null)
        {
            Assert.That(receivedTraceIds, Is.EqualTo(Enumerable.Repeat(activity.TraceId, seenTraceIdsOnSender.Count)));
        }

        Assert.That(receivedDownstreamContextDatas, Has.Count.EqualTo(seenTraceIdsOnSender.Count));
        foreach (var receivedDownstreamContextData in receivedDownstreamContextDatas)
        {
            if (testCase.HasDownstreamData)
            {
                Assert.That(receivedDownstreamContextData, Is.SupersetOf(ContextDataDownstreamAcrossTransports));
            }
            else
            {
                Assert.That(receivedDownstreamContextData.Intersect(ContextDataDownstreamAcrossTransports), Is.Empty);
            }
        }

        Assert.That(receivedBidirectionalContextDatas, Has.Count.EqualTo(seenTraceIdsOnSender.Count));
        foreach (var receivedBidirectionalContextData in receivedBidirectionalContextDatas)
        {
            if (testCase.HasBidirectionalData)
            {
                Assert.That(receivedBidirectionalContextData, Is.EquivalentTo(ContextDataDownstreamBidirectionalAcrossTransports));
            }
            else
            {
                Assert.That(receivedBidirectionalContextData, Is.Empty);
            }
        }

        if (testCase.HasUpstreamData)
        {
            Assert.That(conquerorContext.UpstreamContextData.AsKeyValuePairs(), Is.EquivalentTo(ContextDataUpstreamAcrossTransports));
        }
        else
        {
            Assert.That(conquerorContext.UpstreamContextData, Is.Empty);
        }

        if (testCase.HasBidirectionalData)
        {
            Assert.That(
                conquerorContext.ContextData.AsKeyValuePairs(),
                Is.EquivalentTo(ContextDataUpstreamBidirectionalAcrossTransports
                                    .Concat(ContextDataDownstreamBidirectionalAcrossTransports)
                                    .Concat(InProcessContextData)));
        }
        else
        {
            Assert.That(conquerorContext.ContextData, Is.Empty);
        }
    }

    [Test]
    public void GivenTransport_WhenGettingTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateTestCases().ToList();

        List<Expression<Func<IMessageTransportConformityContextTestCase<TTestHost>, bool>>> predicates =
        [
            testCase => testCase.HasActivity,
            testCase => !testCase.HasActivity,
            testCase => testCase.HasDownstreamData,
            testCase => !testCase.HasDownstreamData,
            testCase => testCase.HasBidirectionalData,
            testCase => !testCase.HasBidirectionalData,
            testCase => testCase.HasUpstreamData,
            testCase => !testCase.HasUpstreamData,
        ];

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<IMessageTransportConformityContextTestCase<TTestHost>>(tc => predicate.Compile().Invoke(tc)),
                    $"missing expected test case: {predicate.Body}");
            }
        });
    }

    private static IEnumerable<TestCaseData> CreateTestCasesPrivate()
        => TTestClass.CreateTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));
}
