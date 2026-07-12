namespace Conqueror.Transport.ConformityTests.Messaging;

using static TestContextData;

[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "by design")]
public abstract class MessageTransportContextConformityTests<TTestClass, TTestHost, TTestCase>
    where TTestClass : MessageTransportContextConformityTests<TTestClass, TTestHost, TTestCase>,
    IMessageTransportContextConformityTests<TTestHost, TTestCase>
    where TTestHost : IMessageTransportConformityTestHost
    where TTestCase : IMessageTransportConformityContextTestCase<TTestHost>
{
    [Test]
    [TestCaseSource(nameof(CreateTestCasesPrivate))]
    public async Task GivenContextData_WhenSendingMessage_DataIsCorrectlySent(TTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        var receivedMessageIds = new ConcurrentQueue<string?>();
        var receivedTraceIds = new ConcurrentQueue<string>();

        var receivedDownstreamContextDatas = new ConcurrentQueue<IReadOnlyCollection<(string, string)>>();
        var receivedBidirectionalContextDatas = new ConcurrentQueue<IReadOnlyCollection<(string, string)>>();

        await using var receiverHost = await host.CreateReceiverTestHost(
            host.TestTimeoutToken,
            (_, ctx, _) =>
            {
                receivedMessageIds.Enqueue(ctx.MessageId);
                receivedTraceIds.Enqueue(ctx.TraceId);

                receivedDownstreamContextDatas.Enqueue(ctx.TransportableData.GetAll().ToList());
                receivedBidirectionalContextDatas.Enqueue(
                    ctx.TransportableData.GetAll(ConquerorContextDataFlowDirection.Bidirectional).ToList()
                );

                if (testCase.HasUpstreamData)
                {
                    foreach (var item in ContextDataUpstreamAcrossTransports)
                    {
                        ctx?.TransportableData.Set(item.Key, item.Value, ConquerorContextDataFlowDirection.Upstream);
                    }

                    foreach (var item in InProcessContextData)
                    {
                        ctx?.InProcessData.Set(item.Key, item.Value, ConquerorContextDataFlowDirection.Upstream);
                    }
                }

                if (testCase.HasBidirectionalData)
                {
                    foreach (var item in ContextDataUpstreamBidirectionalAcrossTransports)
                    {
                        ctx?.TransportableData.Set(
                            item.Key,
                            item.Value,
                            ConquerorContextDataFlowDirection.Bidirectional
                        );
                    }
                }

                return Task.CompletedTask;
            }
        );

        _ = receiverHost.ReceiverExecutionHandle?.CompletionTask.ContinueWith(
            (t, logger) =>
            {
                ((ILogger)logger!).LogError(t.Exception, "error in run");
            },
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
        );

        var seenMessageIdsOnSender = new ConcurrentQueue<string?>();
        var seenTraceIdsOnSender = new ConcurrentQueue<string>();

        DisposableActivity? activity = null;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var senderHost = await host.CreateSenderTestHost(
            cts.Token,
            (_, ctx, _) =>
            {
                seenMessageIdsOnSender.Enqueue(ctx.MessageId);
                seenTraceIdsOnSender.Enqueue(ctx.TraceId);

                return Task.CompletedTask;
            }
        );

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        await testCase.BeforeSend(host);

        if (testCase.HasActivity)
        {
            activity = DisposableActivity.Create(nameof(GivenContextData_WhenSendingMessage_DataIsCorrectlySent));
            _ = activity.Activity.Start();
        }

        using var conquerorContext = senderHost.ConquerorContextAccessor.GetOrCreate();

        using var d = activity;

        if (testCase.HasDownstreamData)
        {
            foreach (var (key, value) in ContextDataDownstreamAcrossTransports)
            {
                conquerorContext.TransportableData.Set(key, value);
            }

            foreach (var (key, value) in InProcessContextData)
            {
                conquerorContext.InProcessData.Set(key, value);
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

        _ = await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken);

        Assert.That(
            () => receivedMessageIds,
            Is.EqualTo(seenMessageIdsOnSender)
                .After(host.AssertionTimeoutInMs)
                .MilliSeconds.PollEvery(milliSeconds: 10)
                .MilliSeconds
        );

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
                Assert.That(
                    receivedDownstreamContextData,
                    Is.SupersetOf(ContextDataDownstreamAcrossTransports.Select(p => (p.Key, p.Value)))
                );
            }
            else
            {
                Assert.That(
                    receivedDownstreamContextData.Intersect(
                        ContextDataDownstreamAcrossTransports.Select(p => (p.Key, p.Value))
                    ),
                    Is.Empty
                );
            }
        }

        Assert.That(receivedBidirectionalContextDatas, Has.Count.EqualTo(seenTraceIdsOnSender.Count));
        foreach (var receivedBidirectionalContextData in receivedBidirectionalContextDatas)
        {
            if (testCase.HasBidirectionalData)
            {
                Assert.That(
                    receivedBidirectionalContextData,
                    Is.EquivalentTo(ContextDataDownstreamBidirectionalAcrossTransports.Select(p => (p.Key, p.Value)))
                );
            }
            else
            {
                Assert.That(receivedBidirectionalContextData, Is.Empty);
            }
        }

        if (testCase.HasUpstreamData)
        {
            Assert.That(
                conquerorContext.TransportableData.GetAll(ConquerorContextDataFlowDirection.Upstream),
                Is.EquivalentTo(ContextDataUpstreamAcrossTransports.Select(p => (p.Key, p.Value)))
            );
        }
        else
        {
            Assert.That(
                conquerorContext.TransportableData.GetAll(ConquerorContextDataFlowDirection.Upstream),
                Is.Empty
            );
        }

        if (testCase.HasBidirectionalData)
        {
            Assert.That(
                conquerorContext.TransportableData.GetAll(ConquerorContextDataFlowDirection.Bidirectional),
                Is.EquivalentTo(
                    ContextDataUpstreamBidirectionalAcrossTransports
                        .Concat(ContextDataDownstreamBidirectionalAcrossTransports)
                        .Select(p => (p.Key, p.Value))
                )
            );
        }
        else
        {
            Assert.That(
                conquerorContext.TransportableData.GetAll(ConquerorContextDataFlowDirection.Bidirectional),
                Is.Empty
            );
        }
    }

    [Test]
    public void GivenTransport_WhenGettingTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateTestCases().ToList();

        var predicates = new List<Expression<Func<IMessageTransportConformityContextTestCase<TTestHost>, bool>>>
        {
            testCase => testCase.HasActivity,
            testCase => !testCase.HasActivity,
            testCase => testCase.HasDownstreamData,
            testCase => !testCase.HasDownstreamData,
            testCase => testCase.HasBidirectionalData,
            testCase => !testCase.HasBidirectionalData,
            testCase => testCase.HasUpstreamData,
            testCase => !testCase.HasUpstreamData,
        };

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<IMessageTransportConformityContextTestCase<TTestHost>>(tc =>
                        predicate.Compile().Invoke(tc)
                    ),
                    $"missing expected test case: {predicate.Body}"
                );
            }
        });
    }

    private static IEnumerable<TestCaseData> CreateTestCasesPrivate() =>
        TTestClass.CreateTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));
}
