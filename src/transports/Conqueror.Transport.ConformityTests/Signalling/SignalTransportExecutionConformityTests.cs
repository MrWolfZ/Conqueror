namespace Conqueror.Transport.ConformityTests.Signalling;

public abstract class SignalTransportExecutionConformityTests<TTestClass, TTestHost, TSuccessTestCase, TErrorTestCase>
    where TTestClass : SignalTransportExecutionConformityTests<TTestClass, TTestHost, TSuccessTestCase, TErrorTestCase>,
    ISignalTransportExecutionConformityTests<TTestHost, TSuccessTestCase, TErrorTestCase>
    where TTestHost : ISignalTransportConformityTestHost
    where TSuccessTestCase : ISignalTransportConformityExecutionSuccessTestCase<TTestHost>
    where TErrorTestCase : ISignalTransportConformityExecutionErrorTestCase<TTestHost>
{
    [Test]
    [TestCaseSource(nameof(CreateSuccessTestCasesPrivate))]
    public async Task GivenTestCase_WhenRunningReceivers_ReceiversReceiveCorrectSignals(TSuccessTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedSignals = new ConcurrentQueue<object>();

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        _ = receiverHost.ReceiverExecutionHandle?.CompletionTask.ContinueWith(
            static (t, l) => ((ILogger)l!).LogError(t.Exception, "error in run"),
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
        );

        if (testCase.ShouldCompleteImmediately)
        {
            var runTask = receiverHost.ReceiverExecutionHandle?.CompletionTask ?? Task.CompletedTask;
            Assert.That(
                () => runTask.IsCompletedSuccessfully,
                Is.True.After(host.AssertionTimeoutInMs).MilliSeconds.PollEvery(milliSeconds: 10).MilliSeconds
            );

            return;
        }

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        await testCase.BeforePublish(host);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        AssertReceivedSignals(receivedSignals, testCase, host);

        await testCase.AfterSignalsAreReceived(host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );
    }

    [Test]
    [TestCaseSource(nameof(CreateSimpleSuccessTestCasesPrivate))]
    public async Task GivenTestCase_WhenRunningReceiversMultipleTimesConcurrently_SignalsAreReceivedCorrectly(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var combinedReceivedSignals = new ConcurrentQueue<object>();
        var receivedSignals1 = new ConcurrentQueue<object>();
        var receivedSignals2 = new ConcurrentQueue<object>();

        await using var receiverHost1 = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                combinedReceivedSignals.Enqueue(signal);
                receivedSignals1.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        await using var receiverHost2 = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                combinedReceivedSignals.Enqueue(signal);
                receivedSignals2.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        await Assert.ThatAsync(
            () =>
                Task.WhenAll(
                        receiverHost1.ReceiverExecutionHandle?.InitialConnectionTask ?? Task.CompletedTask,
                        receiverHost2.ReceiverExecutionHandle?.InitialConnectionTask ?? Task.CompletedTask
                    )
                    .WaitAsync(host.AssertionTimeout, TimeProvider.System, host.TestTimeoutToken),
            Throws.Nothing
        );

        await testCase.BeforePublish(host);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        if (TTestClass.TransportUsesCompetingConsumers)
        {
            AssertReceivedSignals(combinedReceivedSignals, testCase, host, allowOutOfOrder: true);
        }
        else
        {
            AssertReceivedSignals(receivedSignals1, testCase, host);
            AssertReceivedSignals(receivedSignals2, testCase, host);
        }

        await testCase.AfterSignalsAreReceived(host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost1.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        await Assert.ThatAsync(
            () =>
                receiverHost2.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );
    }

    [Test]
    [TestCaseSource(nameof(CreateSimpleSuccessTestCasesPrivate))]
    public async Task GivenTestCase_WhenRunningAndStoppingReceiversMultipleTimes_SignalsAreReceivedMultipleTimes(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedSignals1 = new ConcurrentQueue<object>();

        await using var receiverHost1 = await host.CreateReceiverTestHost(
            cts1.Token,
            (signal, _, _) =>
            {
                receivedSignals1.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        await Assert.ThatAsync(
            () =>
                receiverHost1.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        await testCase.BeforePublish(host);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        AssertReceivedSignals(receivedSignals1, testCase, host);

        await cts1.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost1.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        // publish some signals during downtime to assert whether they are received or not
        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        await Task.Delay(host.ShortDelay, TimeProvider.System, host.TestTimeoutToken); // ensure that the signals are published before restarting the receivers

        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedSignals2 = new ConcurrentQueue<object>();

        await using var receiverHost2 = await host.CreateReceiverTestHost(
            cts2.Token,
            (signal, _, _) =>
            {
                receivedSignals2.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        await Assert.ThatAsync(
            () =>
                receiverHost2.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        await testCase.BeforePublish(host);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        AssertReceivedSignals(
            receivedSignals2,
            testCase,
            host,
            TTestClass.TransportBuffersSignalsDuringReceiverDowntime ? 2 : 1
        );

        await cts2.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost2.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );
    }

    [Test]
    [TestCaseSource(nameof(CreateShutdownTestCasesPrivate))]
    public async Task GivenTestCase_WhenShuttingDownReceiverHost_PerformsCleanShutdown(
        TSuccessTestCase testCase,
        bool useCancel
    )
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedSignals = new ConcurrentQueue<object>();

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        _ = receiverHost.ReceiverExecutionHandle?.CompletionTask.ContinueWith(
            static (t, l) => ((ILogger)l!).LogError(t.Exception, "error in run"),
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
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

        await testCase.BeforePublish(host);

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        AssertReceivedSignals(receivedSignals, testCase, host);

        if (useCancel)
        {
            await cts.CancelAsync();
        }
        else
        {
            if (receiverHost.ReceiverExecutionHandle is { } handle)
            {
                await handle.DisposeAsync();
            }

            // ReSharper disable once DisposeOnUsingVariable (testing this case explicitly)
            await receiverHost.DisposeAsync();
        }

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );
    }

    [Test]
    [TestCaseSource(nameof(CreateSimpleSuccessTestCasesPrivate))]
    public async Task GivenTestCase_WhenCancellingPublish_CallerReceivesOperationCanceledExceptionAndReceiversReceiveNoSignals(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var publisherHost = await host.CreatePublisherTestHost(
            host.TestTimeoutToken,
            (_, _, ct) => tcs.Task.WaitAsync(ct)
        );

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        _ = receiverHost.ReceiverExecutionHandle?.CompletionTask.ContinueWith(
            static (t, l) => ((ILogger)l!).LogError(t.Exception, "error in run"),
            host.Logger,
            host.TestTimeoutToken,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
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

        await testCase.BeforePublish(host);

        using var publishCts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var publishTask = testCase.PublishSignals(publisherHost.SignalPublishers, publishCts.Token);

        await publishCts.CancelAsync();

        tcs.SetResult();

        await Assert.ThatAsync(
            () => publishTask.WaitAsync(host.AssertionTimeout, TimeProvider.System, host.TestTimeoutToken),
            Throws.InstanceOf<OperationCanceledException>()
        );

        await Task.Delay(host.ShortDelay, TimeProvider.System, host.TestTimeoutToken); // give any potential erroneous publish operations time to complete

        Assert.That(receivedSignals, Is.Empty);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );
    }

    [Test]
    [TestCaseSource(nameof(CreateErrorTestCasesPrivate))]
    [Repeat(count: 10)] // we repeat this test many times to catch any potential race conditions in the error handling
    public async Task GivenReceivers_WhenErrorsOccur_CorrectBehaviorIsExecuted(TErrorTestCase testCase)
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        var receivedSignals = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var signalTransportType = new SignalTransportType(TTestClass.TransportTypeName, SignalTransportRole.Receiver);

        if (testCase.ReceiverConfigurationException is { } configurationException)
        {
            await Assert.ThatAsync(
                () => host.CreateReceiverTestHost(cts.Token),
                Throws
                    .InstanceOf<SignalReceiverExecutionFailedException>()
                    .With.InnerException.SameAs(configurationException)
                    .And.Property(nameof(SignalReceiverExecutionFailedException.SignalTransportType))
                    .EqualTo(signalTransportType)
            );

            await testCase.OnReceiverConfigurationException(host);

            return;
        }

        host.Logger.LogInformation("Running receivers...");

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (signal, _, _) =>
            {
                receivedSignals.Enqueue(signal);

                return Task.CompletedTask;
            }
        );

        await testCase.OnInitialReceiverConnection(host);

        await Task.Delay(host.ShortDelay, TimeProvider.System, host.TestTimeoutToken); // give the connections time to start properly

        if (testCase.NumOfExpectedUnrecoverableConnectionErrors > 0)
        {
            await Assert.ThatAsync(
                () =>
                    receiverHost.ReceiverExecutionHandle!.InitialConnectionTask.WaitAsync(
                        host.AssertionTimeout,
                        TimeProvider.System,
                        cts.Token
                    ),
                Throws
                    .InstanceOf<SignalReceiverExecutionFailedException>()
                    .With.Property(nameof(SignalReceiverExecutionFailedException.SignalTransportType))
                    .EqualTo(signalTransportType)
                    .Or.InstanceOf<AggregateException>()
                    .With.Property("InnerExceptions")
                    .Count.EqualTo(testCase.NumOfExpectedUnrecoverableConnectionErrors)
                    .With.Property("InnerExceptions")
                    .Matches<ReadOnlyCollection<Exception>>(exs =>
                        exs.All(ex =>
                            ex is SignalReceiverExecutionFailedException e
                            && e.SignalTransportType == signalTransportType
                        )
                    )
            );

            await Assert.ThatAsync(
                () =>
                    receiverHost.ReceiverExecutionHandle!.CompletionTask.WaitAsync(
                        host.AssertionTimeout,
                        TimeProvider.System,
                        cts.Token
                    ),
                Throws
                    .InstanceOf<SignalReceiverExecutionFailedException>()
                    .With.Property(nameof(SignalReceiverExecutionFailedException.SignalTransportType))
                    .EqualTo(signalTransportType)
                    .Or.InstanceOf<AggregateException>()
                    .With.Property("InnerExceptions")
                    .Count.EqualTo(testCase.NumOfExpectedUnrecoverableConnectionErrors)
                    .With.Property("InnerExceptions")
                    .Matches<ReadOnlyCollection<Exception>>(exs =>
                        exs.All(ex =>
                            ex is SignalReceiverExecutionFailedException e
                            && e.SignalTransportType == signalTransportType
                        )
                    )
            );
        }

        host.Logger.LogInformation("Publishing initial signals...");

        if (testCase.PublishException is { } publishException)
        {
            // we expect the test case to internally handle that the publish exception is thrown
            await Assert.ThatAsync(
                () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
                Throws.InstanceOf<SignalFailedException>().With.InnerException.SameAs(publishException)
            );

            await testCase.OnPublishException(host);

            Assert.That(receivedSignals, Is.Empty);

            return;
        }

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        AssertReceivedSignals(receivedSignals, testCase, host);

        // return if no active receivers are expected
        if (testCase.ExpectedReceivedSignals.Count is 0)
        {
            return;
        }

        // at this point, all handlers should have connected successfully
        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );

        var handlerExceptions = testCase
            .HandlerExceptions.OfType<Exception>()
            .OrderBy(ex => ex.Message, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (handlerExceptions.Count > 0)
        {
            await Assert.ThatAsync(
                () =>
                    receiverHost.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                        host.AssertionTimeout,
                        TimeProvider.System,
                        cts.Token
                    ) ?? Task.CompletedTask,
                Throws
                    .InstanceOf<SignalReceiverExecutionFailedException>()
                    .With.InnerException.Matches<Exception>(e => handlerExceptions.Contains(e))
                    .And.Property(nameof(SignalReceiverExecutionFailedException.SignalTransportType))
                    .EqualTo(signalTransportType)
                    .Or.InstanceOf<AggregateException>()
                    .With.Property("InnerExceptions")
                    .Count.EqualTo(handlerExceptions.Count)
                    .With.Property("InnerExceptions")
                    .Matches<ReadOnlyCollection<Exception>>(exs =>
                        exs.OfType<SignalReceiverExecutionFailedException>()
                            .Select(ex => ex.InnerException)
                            .OfType<Exception>()
                            .OrderBy(ex => ex.Message, StringComparer.OrdinalIgnoreCase)
                            .SequenceEqual(handlerExceptions)
                    )
            );

            await testCase.OnHandlerExceptions(host);

            // the initial connection task should not be affected by handler exceptions
            Assert.That(
                receiverHost.ReceiverExecutionHandle?.InitialConnectionTask.IsCompletedSuccessfully ?? true,
                Is.True
            );

            return;
        }

        if (!TTestClass.TransportSupportsReconnectingReceivers)
        {
            host.Logger.LogInformation("Transport does not support reconnecting; skipping reconnect tests...");

            return;
        }

        host.Logger.LogInformation("Triggering reconnects...");

        await testCase.TriggerReconnect(host);

        // we expect reconnection to be immediate
        await testCase.AfterSuccessfulReconnect(host);

        host.Logger.LogInformation("Publishing signals after reconnects...");

        receivedSignals.Clear();

        await Assert.ThatAsync(
            () => testCase.PublishSignals(publisherHost.SignalPublishers, host.TestTimeoutToken),
            Throws.Nothing
        );

        AssertReceivedSignals(receivedSignals, testCase, host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle?.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ) ?? Task.CompletedTask,
            Throws.Nothing
        );
    }

    [Test]
    public void GivenTransport_WhenGettingSuccessTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateSuccessTestCases().ToList();

        var predicates = new List<Expression<Func<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>, bool>>>
        {
            testCase => testCase.ShouldCompleteImmediately,
            testCase => testCase.SignalsArePublishedInParallel && testCase.NumOfReceivers == 1,
            testCase => !testCase.SignalsArePublishedInParallel && testCase.NumOfReceivers == 1,
            testCase => testCase.SignalsArePublishedInParallel && testCase.NumOfReceivers > 1,
            testCase => !testCase.SignalsArePublishedInParallel && testCase.NumOfReceivers > 1,
            testCase => testCase.ExpectedReceivedSignals.Count == 0,
            testCase => testCase.ExpectedReceivedSignals.Count > 0,
            testCase => testCase.ExpectedReceivedSignals.Count > 1,
            testCase =>
                testCase.ExpectedReceivedSignals.Any(s =>
                    s.GetType()
                        .GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                        .Any(p =>
                            p.Name.EndsWith(".JsonSerializerContext", StringComparison.InvariantCulture)
                            && p.GetValue(s) != null
                        )
                ),
            testCase =>
                testCase.ExpectedReceivedSignals.Any(s =>
                    s.GetType()
                        .GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                        .Any(p =>
                            p.Name.EndsWith(".EmptyInstance", StringComparison.InvariantCulture)
                            && p.GetValue(s) != null
                        )
                ),
            testCase =>
                testCase.ExpectedReceivedSignals.Any(s =>
                    s.GetType().Name.EndsWith("ForAssemblyScanning", StringComparison.InvariantCulture)
                ),
            testCase =>
                testCase.ExpectedReceivedSignals.Any(s =>
                    s.GetType().Name.EndsWith("WithDelegateHandler", StringComparison.InvariantCulture)
                ),
            testCase =>
                testCase.ExpectedReceivedSignals.Any(s =>
                    s.GetType().BaseType != null
                    && s.GetType()
                        .BaseType!.GetCustomAttributes()
                        .Any(a => a.GetType().Name.EndsWith("SignalAttribute", StringComparison.InvariantCulture))
                ),
        };

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>>(tc =>
                        predicate.Compile().Invoke(tc)
                    ),
                    $"missing expected test case: {predicate.Body}"
                );
            }
        });
    }

    [Test]
    public void GivenTransport_WhenGettingSimpleSuccessTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateSimpleSuccessTestCases().ToList();

        var predicates = new List<Expression<Func<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>, bool>>>
        {
            testCase => testCase.NumOfReceivers == 1,
            testCase => testCase.NumOfReceivers > 1,
        };

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityExecutionSuccessTestCase<TTestHost>>(tc =>
                        predicate.Compile().Invoke(tc)
                    ),
                    $"missing expected test case: {predicate.Body}"
                );
            }
        });
    }

    [Test]
    public void GivenTransport_WhenGettingErrorTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateErrorTestCases().ToList();

        var predicates = new List<Expression<Func<ISignalTransportConformityExecutionErrorTestCase<TTestHost>, bool>>>
        {
            testCase => testCase.ReceiverConfigurationException != null,
            testCase => testCase.PublishException != null,
            testCase => !testCase.HandlerExceptions.OfType<Exception>().Any(),
            testCase => testCase.HandlerExceptions.OfType<Exception>().Take(2).Count() == 1,
            testCase => testCase.HandlerExceptions.OfType<Exception>().Skip(1).Any(),
            testCase =>
                testCase.NumOfExpectedUnrecoverableConnectionErrors == 0
                || testCase.NumOfExpectedUnrecoverableConnectionErrors == null,
            testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors == 1,
            testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors > 1,
        };

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<ISignalTransportConformityExecutionErrorTestCase<TTestHost>>(tc =>
                        predicate.Compile().Invoke(tc)
                    ),
                    $"missing expected test case: {predicate.Body}"
                );
            }
        });
    }

    private static void AssertReceivedSignals(
        ConcurrentQueue<object> receivedSignals,
        ISignalTransportConformityExecutionTestCase<TTestHost> testCase,
        ISignalTransportConformityTestHost host,
        int numOfRepeats = 1,
        bool allowOutOfOrder = false
    )
    {
        if (testCase.NumOfReceivers > 1 || testCase.SignalsArePublishedInParallel || allowOutOfOrder)
        {
            // with multiple receivers or when publishing in parallel, the order of signals is not
            // guaranteed, so we use EquivalentTo instead of EqualTo
            Assert.That(
                () => receivedSignals,
                Is.EquivalentTo(Enumerable.Repeat(testCase.ExpectedReceivedSignals, numOfRepeats).SelectMany(e => e))
                    .After(host.AssertionTimeoutInMs)
                    .MilliSeconds.PollEvery(milliSeconds: 10)
                    .MilliSeconds
            );
        }
        else
        {
            Assert.That(
                () => receivedSignals,
                Is.EqualTo(Enumerable.Repeat(testCase.ExpectedReceivedSignals, numOfRepeats).SelectMany(e => e))
                    .After(host.AssertionTimeoutInMs)
                    .MilliSeconds.PollEvery(milliSeconds: 10)
                    .MilliSeconds
            );
        }
    }

    private static IEnumerable<TestCaseData> CreateSuccessTestCasesPrivate() =>
        TTestClass.CreateSuccessTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private static IEnumerable<TestCaseData> CreateSimpleSuccessTestCasesPrivate() =>
        TTestClass.CreateSimpleSuccessTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private static IEnumerable<TestCaseData> CreateShutdownTestCasesPrivate() =>
        new[] { true, false }.SelectMany(b =>
            TTestClass
                .CreateSimpleSuccessTestCases()
                .Select(tc => new TestCaseData(tc, b).SetName($"{tc.Name} with useCancel={b}"))
        );

    private static IEnumerable<TestCaseData> CreateErrorTestCasesPrivate() =>
        TTestClass.CreateErrorTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));
}
