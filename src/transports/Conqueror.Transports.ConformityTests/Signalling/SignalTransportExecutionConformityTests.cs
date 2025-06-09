using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Conqueror.Transports.ConformityTests.Signalling;

public abstract class SignalTransportExecutionConformityTests<TTestClass, TTestHost, TSuccessTestCase, TErrorTestCase>
    where TTestClass : SignalTransportExecutionConformityTests<TTestClass, TTestHost, TSuccessTestCase, TErrorTestCase>,
    ISignalTransportExecutionConformityTests<TTestHost, TSuccessTestCase, TErrorTestCase>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
    where TSuccessTestCase : ISignalTransportConformityExecutionSuccessTestCase<TTestHost>
    where TErrorTestCase : ISignalTransportConformityExecutionErrorTestCase<TTestHost>
{
    [Test]
    [TestCaseSource(nameof(CreateSuccessTestCasesPrivate))]
    public async Task GivenTestCase_WhenRunningReceivers_ReceiversReceiveCorrectSignals(TSuccessTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run = testCase.RunReceivers(
            host.SignalReceivers,
            cts.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        _ = run.CompletionTask.ContinueWith(
            static (t, l) =>
            {
                ((ILogger)l!).LogError(t.Exception!, "error in run");
            },
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        if (testCase.ShouldCompleteImmediately)
        {
            var runTask = run.CompletionTask;
            Assert.That(
                () => runTask.IsCompletedSuccessfully,
                Is.True
                  .After(host.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);

            return;
        }

        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 1);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        await testCase.OnReceiveSuccess(host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => run.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    [TestCaseSource(nameof(CreateSimpleSuccessTestCasesPrivate))]
    public async Task GivenReceiver_WhenRunningReceiverMultipleTimesInParallel_SignalsAreReceivedMultipleTimes(TSuccessTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run1 = testCase.RunReceivers(
            host.SignalReceivers,
            cts.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        await using var run2 = testCase.RunReceivers(
            host.SignalReceivers,
            cts.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        await Assert.ThatAsync(
            () => Task.WhenAll(run1.InitialConnectionTask, run2.InitialConnectionTask).WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 2);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        Assert.That(
            () => receivedSignals,
            Is.EquivalentTo(testCase.ExpectedReceivedSignals.Concat(testCase.ExpectedReceivedSignals))
              .After(host.AssertionTimeoutInMs)
              .MilliSeconds
              .PollEvery(10)
              .MilliSeconds);

        await testCase.OnReceiveSuccess(host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => run1.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await Assert.ThatAsync(
            () => run2.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    [TestCaseSource(nameof(CreateSimpleSuccessTestCasesPrivate))]
    [Repeat(20)] // we repeat this test many times to catch any potential race conditions
    public async Task GivenReceiver_WhenRunningAndStoppingReceiverMultipleTimes_SignalsAreReceivedMultipleTimes(TSuccessTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run1 = testCase.RunReceivers(
            host.SignalReceivers,
            cts1.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        await Assert.ThatAsync(
            () => run1.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 1);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        await cts1.CancelAsync();
        await run1.CompletionTask;

        // validate that publishing signal during downtime is not received
        await testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken);

        await Task.Delay(100, host.TestTimeoutToken); // ensure that the signal is published before restarting the receiver

        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run2 = testCase.RunReceivers(
            host.SignalReceivers,
            cts2.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        await Assert.ThatAsync(
            () => run2.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 1);

        receivedSignals.Clear();

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        await cts2.CancelAsync();

        await Assert.ThatAsync(
            () => run2.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    [TestCaseSource(nameof(CreateShutdownTestCasesPrivate))]
    public async Task GivenReceiver_WhenCancellingRun_PerformsCleanShutdown(TSuccessTestCase testCase, bool useCancel)
    {
        await using var host = await testCase.CreateTestHost();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run = testCase.RunReceivers(host.SignalReceivers, cts.Token);

        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 1);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        if (useCancel)
        {
            await cts.CancelAsync();
        }
        else
        {
            // ReSharper disable once DisposeOnUsingVariable (testing this case explicitly)
            await run.DisposeAsync();
        }

        await Assert.ThatAsync(
            () => run.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    [TestCaseSource(nameof(CreateSimpleSuccessTestCasesPrivate))]
    public async Task GivenReceiver_WhenCancellingPublish_CallerReceivesExceptionAndReceiverReceivesNoSignals(TSuccessTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run = testCase.RunReceivers(
            host.SignalReceivers,
            cts.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnConnectionSuccess(host, 1);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var publishCts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        var publishToken = publishCts.Token;

        var publishTask = testCase.PublishSignals(host.SignalPublishers, publishToken, (_, _, ct) => tcs.Task.WaitAsync(ct));

        await publishCts.CancelAsync();

        tcs.SetResult();

        await Assert.ThatAsync(() => publishTask, Throws.InstanceOf<OperationCanceledException>());

        await Task.Delay(100, host.TestTimeoutToken); // give any potential erroneous publish operations time to complete

        Assert.That(receivedSignals, Is.Empty);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => run.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    [TestCaseSource(nameof(CreateErrorTestCasesPrivate))]
    [Repeat(20)] // we repeat this test many times to catch any potential race conditions in the error handling
    public async Task GivenReceivers_WhenErrorsOccur_CorrectBehaviorIsExecuted(TErrorTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        if (testCase.ConfigurationException is { } configurationException)
        {
            var ct = host.TestTimeoutToken;
            var signalReceivers = host.SignalReceivers;
            Assert.That(
                () => testCase.RunReceivers(signalReceivers, ct),
                Throws.InstanceOf<SignalReceiverRunFailedException>()
                      .With.InnerException.SameAs(configurationException));

            await testCase.OnConfigurationException(host);

            return;
        }

        host.Logger.LogInformation("Running receivers...");

        await using var run = testCase.RunReceivers(
            host.SignalReceivers,
            cts.Token,
            signalCallback: (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            });

        await testCase.OnInitialConnection(host);

        await Task.Delay(10, host.TestTimeoutToken); // give the connections time to start properly

        if (testCase.NumOfExpectedUnrecoverableConnectionErrors == 1)
        {
            await Assert.ThatAsync(
                () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                Throws.InstanceOf<SignalReceiverRunFailedException>());

            await Assert.ThatAsync(
                () => run.CompletionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                Throws.InstanceOf<SignalReceiverRunFailedException>());
        }

        if (testCase.NumOfExpectedUnrecoverableConnectionErrors > 1)
        {
            // necessary for try/catch below to work
            using var d = new TestExecutionContext.IsolatedContext();

            try
            {
                await Assert.ThatAsync(
                    () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                    Throws.InstanceOf<AggregateException>()
                          .With.Property("InnerExceptions")
                          .Count.EqualTo(testCase.NumOfExpectedUnrecoverableConnectionErrors)
                          .With.Property("InnerExceptions")
                          .Matches<ReadOnlyCollection<Exception>>(exs => exs.All(ex => ex is SignalReceiverRunFailedException)));

                await Assert.ThatAsync(
                    () => run.CompletionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                    Throws.InstanceOf<AggregateException>()
                          .With.Property("InnerExceptions")
                          .Count.EqualTo(testCase.NumOfExpectedUnrecoverableConnectionErrors)
                          .With.Property("InnerExceptions")
                          .Matches<ReadOnlyCollection<Exception>>(exs => exs.All(ex => ex is SignalReceiverRunFailedException)));
            }

            // there is a rare race condition that we cannot prevent where the client receives the first unrecoverable error
            // before receiving the second unrecoverable error; in that case, the second request will be canceled, and therefore
            // only a single exception will be thrown; in that case the assertions below should succeed, and if the assertion failure
            // was due to some other reason (e.g. no exception was thrown), then the assertions will simply fail again
            catch (AssertionException)
            {
                await Assert.ThatAsync(
                    () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                    Throws.InstanceOf<SignalReceiverRunFailedException>());

                await Assert.ThatAsync(
                    () => run.CompletionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                    Throws.InstanceOf<SignalReceiverRunFailedException>());
            }
        }

        host.Logger.LogInformation("Publishing initial signals...");

        if (testCase.PublishException is { } publishException)
        {
            await Assert.ThatAsync(
                () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
                Throws.Exception.SameAs(publishException));

            await testCase.OnPublishException(host);

            Assert.That(receivedSignals, Is.Empty);

            return;
        }

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        // return if no active receivers are expected
        if (testCase.ExpectedReceivedSignals.Count == 0)
        {
            return;
        }

        // at this point, all handlers should have connected successfully
        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        var handlerExceptions = testCase.HandlerExceptions
                                        .OfType<Exception>()
                                        .OrderBy(ex => ex.Message)
                                        .ToList();

        if (handlerExceptions.Count == 1)
        {
            await Assert.ThatAsync(
                () => run.CompletionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                Throws.InstanceOf<SignalReceiverRunFailedException>()
                      .With.InnerException.SameAs(handlerExceptions[0]));
        }

        if (handlerExceptions.Count > 1)
        {
            // necessary for try/catch below to work
            using var d = new TestExecutionContext.IsolatedContext();

            try
            {
                await Assert.ThatAsync(
                    () => run.CompletionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                    Throws.InstanceOf<AggregateException>()
                          .With.Property("InnerExceptions")
                          .Count.EqualTo(handlerExceptions.Count)
                          .With.Property("InnerExceptions")
                          .Matches<ReadOnlyCollection<Exception>>(exs => exs.OfType<SignalReceiverRunFailedException>()
                                                                            .Select(ex => ex.InnerException)
                                                                            .OfType<Exception>()
                                                                            .OrderBy(ex => ex.Message)
                                                                            .SequenceEqual(handlerExceptions)));
            }

            // there is a rare race condition that we cannot prevent where the first handler throws before the second handler
            // receives the signal; in that case, the second request will be canceled, and therefore only a single exception
            // will be thrown; in that case the assertion below should succeed, and if the assertion failure
            // was due to some other reason (e.g. no exception was thrown), then the assertion will simply fail again
            catch (AssertionException)
            {
                await Assert.ThatAsync(
                    () => run.CompletionTask.WaitAsync(host.AssertionTimeout, cts.Token),
                    Throws.InstanceOf<SignalReceiverRunFailedException>());
            }
        }

        if (handlerExceptions.Count > 0)
        {
            await testCase.OnHandlerExceptions(host);

            // the initial connection task should not be affected by handler exceptions
            Assert.That(run.InitialConnectionTask.IsCompletedSuccessfully, Is.True);

            return;
        }

        host.Logger.LogInformation("Triggering reconnects...");

        await testCase.TriggerReconnect(host);

        // we expect reconnection to be immediate
        await testCase.AfterSuccessfulReconnect(host);

        host.Logger.LogInformation("Publishing signals after reconnects...");

        receivedSignals.Clear();

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => run.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    [TestCaseSource(nameof(CreateReconnectDelayTestCasesPrivate))]
    public async Task GivenReceiverWithReconnectDelayFn_WhenRunningReceiverWithRecoverableErrors_ReconnectsAreExecutedAfterDelay(
        TErrorTestCase testCase)
    {
        await using var host = await testCase.CreateTestHost();

        var receivedSignals = new ConcurrentQueue<object>();

        var taskCompletionSource1 = new TaskCompletionSource();
        var taskCompletionSource2 = new TaskCompletionSource();
        var taskCompletionSources = new Queue<TaskCompletionSource>([taskCompletionSource1, taskCompletionSource2]);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var run = testCase.RunReceivers(
            host.SignalReceivers,
            cts.Token,
            signalCallback: (signal, _, _) =>
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

        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.TypeOf<TimeoutException>());

        host.Logger.LogInformation("Publishing initial signals...");

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        await Task.Delay(10, host.TestTimeoutToken);

        Assert.That(receivedSignals, Is.Empty);

        taskCompletionSource1.SetResult();

        await Assert.ThatAsync(
            () => run.InitialConnectionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);

        await testCase.OnInitialConnection(host);

        host.Logger.LogInformation("Publishing signals after initial connection...");

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        await testCase.TriggerReconnect(host);

        await Task.Delay(10, host.TestTimeoutToken);

        // the connection task is not influenced by reconnections
        Assert.That(run.InitialConnectionTask.IsCompletedSuccessfully, Is.True);

        taskCompletionSource2.SetResult();

        await testCase.AfterSuccessfulReconnect(host);

        host.Logger.LogInformation("Publishing signals after reconnects...");

        receivedSignals.Clear();

        await Assert.ThatAsync(
            () => testCase.PublishSignals(host.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing);

        AssertReceivedSignals(receivedSignals, testCase, host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () => run.CompletionTask.WaitAsync(host.AssertionTimeout, host.TestTimeoutToken),
            Throws.Nothing);
    }

    [Test]
    public void GivenTransport_WhenGettingSuccessTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateSuccessTestCases().ToList();

        List<Expression<Func<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>, bool>>> predicates =
        [
            testCase => testCase.ShouldCompleteImmediately,
            testCase => testCase.ExpectedReceivedSignals.Count == 0,
            testCase => testCase.ExpectedReceivedSignals.Count > 0,
        ];

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>>(tc => predicate.Compile().Invoke(tc)),
                    $"missing expected test case: {predicate.Body}");
            }
        });
    }

    [Test]
    public void GivenTransport_WhenGettingSimpleSuccessTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateSimpleSuccessTestCases().ToList();

        List<Expression<Func<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>, bool>>> predicates =
        [
            testCase => testCase.NumOfReceivers == 1,
            testCase => testCase.NumOfReceivers > 1,
        ];

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>>(tc => predicate.Compile().Invoke(tc)),
                    $"missing expected test case: {predicate.Body}");
            }
        });
    }

    [Test]
    public void GivenTransport_WhenGettingErrorTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateErrorTestCases().ToList();

        List<Expression<Func<ISignalTransportConformityExecutionErrorTestCase<TTestHost>, bool>>> predicates =
        [
            testCase => testCase.ConfigurationException != null,
            testCase => testCase.PublishException != null,
            testCase => !testCase.HandlerExceptions.OfType<Exception>().Any(),
            testCase => testCase.HandlerExceptions.OfType<Exception>().Count() == 1,
            testCase => testCase.HandlerExceptions.OfType<Exception>().Count() > 1,
            testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors == 0,
            testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors == 1,
            testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors > 1,
        ];

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityExecutionErrorTestCase<TTestHost>>(tc => predicate.Compile().Invoke(tc)),
                    $"missing expected test case: {predicate.Body}");
            }
        });
    }

    private static void AssertReceivedSignals(
        ConcurrentQueue<object> receivedSignals,
        ISignalTransportConformityExecutionTestCase<TTestHost> testCase,
        ISignalTransportConformityTestHost<TTestHost> host)
    {
        if (testCase.NumOfReceivers > 1)
        {
            // with multiple receivers the order of signals is not guaranteed, so we use EquivalentTo instead of EqualTo
            Assert.That(
                () => receivedSignals,
                Is.EquivalentTo(testCase.ExpectedReceivedSignals)
                  .After(host.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);
        }
        else
        {
            Assert.That(
                () => receivedSignals,
                Is.EqualTo(testCase.ExpectedReceivedSignals)
                  .After(host.AssertionTimeoutInMs)
                  .MilliSeconds
                  .PollEvery(10)
                  .MilliSeconds);
        }
    }

    private static IEnumerable<TestCaseData> CreateSuccessTestCasesPrivate()
        => TTestClass.CreateSuccessTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private static IEnumerable<TestCaseData> CreateSimpleSuccessTestCasesPrivate()
        => TTestClass.CreateSimpleSuccessTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private static IEnumerable<TestCaseData> CreateShutdownTestCasesPrivate()
        => new[] { true, false }.SelectMany(b => TTestClass.CreateSimpleSuccessTestCases()
                                                           .Select(tc => new TestCaseData(tc, b).SetName($"{tc.Name} with useCancel={b}")));

    private static IEnumerable<TestCaseData> CreateErrorTestCasesPrivate()
        => TTestClass.CreateErrorTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private static IEnumerable<TestCaseData> CreateReconnectDelayTestCasesPrivate()
        => TTestClass.CreateReconnectDelayTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));
}
