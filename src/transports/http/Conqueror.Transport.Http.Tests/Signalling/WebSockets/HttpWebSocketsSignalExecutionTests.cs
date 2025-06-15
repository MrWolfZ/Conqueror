using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalConformityTestCase;
using static Conqueror.Transport.Http.Tests.Signalling.HttpSignalTestCases;

namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("Structure", "NUnit1028:The non-test method is public", Justification = "test case generation methods must be public")]
public sealed partial class HttpWebSocketsSignalExecutionTests
{
    [Test]
    [Combinatorial]
    public void GivenMultipleHttpWebSocketsSignalTypesWithSameTag_WhenRunningReceivers_ThrowsException([Values] bool runIndividually)
    {
        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .AddSignalHandler<TestSignalWithDuplicateTagHandler>();

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        Assert.That(
            () => runIndividually
                ? signalReceivers.RunHttpWebSocketsSignalReceiver<TestSignalWithDuplicateTagHandler>(CancellationToken.None)
                : signalReceivers.RunHttpWebSocketsSignalReceivers(CancellationToken.None),
            Throws.InstanceOf<SignalReceiverExecutionFailedException>()
                  .With.InnerException.InstanceOf<InvalidOperationException>()
                  .With.InnerException.Message.Contains("is already used by signal type"));
    }

    [Test]
    [TestCaseSource(typeof(HttpSignalTestCases), nameof(CreateSimpleSuccessTestCases), [HttpSignalTransportType.WebSockets])]
    [SuppressMessage(
        "Structure",
        "NUnit1018:The number of parameters provided by the TestCaseSource does not match the number of parameters in the target method",
        Justification = "false positive")]
    public async Task GivenHttpWebSocketsSignalHandlerForMultipleSignalTypes_WhenRunningReceiver_OnlyConfiguresReceiverOnce(
        HttpSignalConformityExecutionSuccessTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        var configCount = 0;

        var clientServices = new ServiceCollection().AddConquerorHttpClient()
                                                    .AddSignalHandler<MultiTestSignalHandler>()
                                                    .AddSingleton<Action<IHttpWebSocketsSignalReceiver>>(r =>
                                                    {
                                                        configCount += 1;
                                                        _ = r.Enable(HttpSignalTransportConformityTestHost.WebSocketsAddress)
                                                             .WithWebSocketFactory(async (address, _)
                                                                                       //// ReSharper disable once AccessToDisposedClosure
                                                                                       => await host.ConnectToWebSocket(address));
                                                    });

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var handle = signalReceivers.RunHttpWebSocketsSignalReceivers(cts.Token);

        await Assert.ThatAsync(
            () => handle.InitialConnectionTask.WaitAsync(host.AssertionTimeout, cts.Token),
            Throws.Nothing);

        Assert.That(configCount, Is.EqualTo(1));
    }

    [Test]
    [TestCaseSource(nameof(CreateReconnectDelayTestCases))]
    public async Task GivenReceiverWithReconnectDelayFn_WhenRunningReceiverWithRecoverableErrors_ReconnectsAreExecutedAfterDelay(
        HttpSignalConformityExecutionErrorTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        var receivedSignals = new ConcurrentQueue<object>();

        var taskCompletionSource1 = new TaskCompletionSource();
        var taskCompletionSource2 = new TaskCompletionSource();
        var taskCompletionSources = new Queue<TaskCompletionSource>([taskCompletionSource1, taskCompletionSource2]);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            },
            reconnectDelayCallback: async ct =>
            {
                if (taskCompletionSources.TryDequeue(out var tcs))
                {
                    await tcs.Task.WaitAsync(ct);
                }
            });

        _ = receiverHost.ReceiverExecutionHandle?.CompletionTask.ContinueWith(
            static (t, l) =>
            {
                ((ILogger)l!).LogError(t.Exception!, "error in run");
            },
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        await Assert.ThatAsync(
            () => receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken) ?? Task.CompletedTask,
            Throws.TypeOf<TimeoutException>());

        host.Logger.LogInformation("Publishing initial signals...");

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        await Task.Delay(host.ShortDelay, host.TestTimeoutToken);

        Assert.That(receivedSignals, Is.Empty);

        taskCompletionSource1.SetResult();

        await Assert.ThatAsync(
            () => receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken) ?? Task.CompletedTask,
            Throws.Nothing);

        await testCase.OnInitialReceiverConnection(host);

        host.Logger.LogInformation("Publishing signals after initial connection...");

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        Assert.That(
            () => receivedSignals,
            Is.EqualTo(testCase.ExpectedReceivedSignals)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await testCase.TriggerReconnect(host);

        await Task.Delay(host.ShortDelay, host.TestTimeoutToken);

        // the connection task is not influenced by reconnections
        Assert.That(receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.IsCompletedSuccessfully ?? true, Is.True);

        taskCompletionSource2.SetResult();

        await testCase.AfterSuccessfulReconnect(host);

        host.Logger.LogInformation("Publishing signals after reconnects...");

        receivedSignals.Clear();

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        Assert.That(
            () => receivedSignals,
            Is.EqualTo(testCase.ExpectedReceivedSignals)
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => receiverHost.ReceiverExecutionHandle?.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken) ?? Task.CompletedTask,
            Throws.Nothing);
    }

    private static IEnumerable<TestCaseData> CreateReconnectDelayTestCases()
        => HttpSignalTestCases.CreateReconnectDelayTestCases(HttpSignalTransportType.WebSockets)
                              .Select(tc => new TestCaseData(tc).SetName(tc.Name));

    [HttpWebSocketsSignal(Tag = "duplicate-tag")]
    private sealed partial record TestSignalWithDuplicateTag1(int Payload);

    [HttpWebSocketsSignal(Tag = "duplicate-tag")]
    private sealed partial record TestSignalWithDuplicateTag2(int Payload);

    private sealed partial class TestSignalWithDuplicateTagHandler
        : TestSignalWithDuplicateTag1.IHandler,
          TestSignalWithDuplicateTag2.IHandler
    {
        public Task Handle(TestSignalWithDuplicateTag1 signal, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        Task TestSignalWithDuplicateTag2.IHandler.Handle(TestSignalWithDuplicateTag2 signal, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        static void IHttpWebSocketsSignalHandler.ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver)
            => receiver.Enable(HttpSignalTransportConformityTestHost.WebSocketsAddress);
    }
}
