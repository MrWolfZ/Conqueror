namespace Conqueror.Transport.ConformityTests.Messaging;

public abstract class MessageTransportExecutionConformityTests<TTestClass, TTestHost, TSuccessTestCase, TErrorTestCase>
    where TTestClass : MessageTransportExecutionConformityTests<
        TTestClass,
        TTestHost,
        TSuccessTestCase,
        TErrorTestCase
    >,
    IMessageTransportExecutionConformityTests<TTestHost, TSuccessTestCase, TErrorTestCase>
    where TTestHost : IMessageTransportConformityTestHost
    where TSuccessTestCase : IMessageTransportConformityExecutionSuccessTestCase<TTestHost>
    where TErrorTestCase : IMessageTransportConformityExecutionErrorTestCase<TTestHost>
{
    [Test]
    [TestCaseSource(nameof(CreateSuccessTestCasesPrivate))]
    public async Task GivenTestCase_WhenRunningReceivers_ReceiversReceiveCorrectMessagesAndReturnCorrectResponses(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedMessages = new ConcurrentQueue<object>();
        var returnedResponses = new List<object>();

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (message, _, _) =>
            {
                receivedMessages.Enqueue(message);

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

        await using var senderHost = await host.CreateSenderTestHost(host.TestTimeoutToken);

        await testCase.BeforeSend(host);

        await Assert.ThatAsync(
            async () =>
                returnedResponses.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        AssertReceivedMessages(receivedMessages, testCase, host);
        AssertReturnedResponses(returnedResponses, testCase, host);

        await testCase.AfterMessagesAreReceived(host);

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
    [TestCaseSource(nameof(CreateTestCasesForConcurrentExecution))]
    public async Task GivenTestCase_WhenRunningReceiversMultipleTimesConcurrently_MessagesAreReceivedByEachReceiver(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedMessages = new ConcurrentQueue<object>();
        var returnedResponses = new List<object>();

        await using var receiverHost1 = await host.CreateReceiverTestHost(
            cts.Token,
            (message, _, _) =>
            {
                receivedMessages.Enqueue(message);

                return Task.CompletedTask;
            }
        );

        await using var receiverHost2 = await host.CreateReceiverTestHost(
            cts.Token,
            (message, _, _) =>
            {
                receivedMessages.Enqueue(message);

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

        await using var senderHost = await host.CreateSenderTestHost(host.TestTimeoutToken);

        await testCase.BeforeSend(host);

        await Assert.ThatAsync(
            async () =>
                returnedResponses.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        AssertReceivedMessages(receivedMessages, testCase, host, allowOutOfOrder: true);

        // even when there are multiple competing receivers, we should only receive one set of responses
        AssertReturnedResponses(returnedResponses, testCase, host);

        await testCase.AfterMessagesAreReceived(host);

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
    public async Task GivenTestCase_WhenRunningAndStoppingReceiversMultipleTimes_MessagesAreReceivedMultipleTimes(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        using var cts1 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedMessages1 = new ConcurrentQueue<object>();
        var returnedResponses1 = new List<object>();

        await using var receiverHost1 = await host.CreateReceiverTestHost(
            cts1.Token,
            (message, _, _) =>
            {
                receivedMessages1.Enqueue(message);

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

        await using var senderHost = await host.CreateSenderTestHost(host.TestTimeoutToken);

        await testCase.BeforeSend(host);

        await Assert.ThatAsync(
            async () =>
                returnedResponses1.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        AssertReceivedMessages(receivedMessages1, testCase, host);
        AssertReturnedResponses(returnedResponses1, testCase, host);

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

        // send some messages during downtime to assert whether they are received or not
        if (TTestClass.TransportBuffersMessagesDuringReceiverDowntime)
        {
            if (testCase.ExpectedResponses.Count is 0)
            {
                // for messages without response, sending them without an active receiver and with
                // buffering should complete fine
                await Assert.ThatAsync(
                    () => testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken),
                    Throws.Nothing
                );
            }
            else
            {
                using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
                sendCts.CancelAfter(host.ShortDelay);

                // for messages with response, sending them without an active receiver and with
                // buffering should time out waiting for the response
                await Assert.ThatAsync(
                    () => testCase.SendMessages(senderHost.MessageSenders, sendCts.Token),
                    Throws
                        .InstanceOf<OperationCanceledException>()
                        .Or.InnerException.InstanceOf<OperationCanceledException>()
                );

                // additional assert to ensure that the exception above is not due to a test timeout
                Assert.That(host.TestTimeoutToken.IsCancellationRequested, Is.False);
            }
        }
        else
        {
            // when messages are not buffered, sending them should lead to some kind of exception
            await Assert.ThatAsync(
                () => testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken),
                Throws.Exception.Not.InstanceOf<OperationCanceledException>()
            );
        }

        await Task.Delay(host.ShortDelay, TimeProvider.System, host.TestTimeoutToken); // ensure that the messages are sent before restarting the receivers

        using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedMessages2 = new ConcurrentQueue<object>();
        var returnedResponses2 = new List<object>();

        await using var receiverHost2 = await host.CreateReceiverTestHost(
            cts2.Token,
            (message, _, _) =>
            {
                receivedMessages2.Enqueue(message);

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

        await testCase.BeforeSend(host);

        await Assert.ThatAsync(
            async () =>
                returnedResponses2.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        var expectedReceivedMessages = new List<object>(testCase.ExpectedReceivedMessages);

        if (TTestClass.TransportBuffersMessagesDuringReceiverDowntime)
        {
            // when we expect sequential responses, then only a single message should be sent during the downtime,
            // since that send operation will time out before any other message can be sent
            if (testCase.ExpectedResponses.Count > 0 && !testCase.MessagesAreSentInParallel)
            {
                expectedReceivedMessages.Add(expectedReceivedMessages[0]);
            }
            else
            {
                expectedReceivedMessages.AddRange([.. expectedReceivedMessages]);
            }
        }

        Assert.That(
            () => receivedMessages2,
            Is.EquivalentTo(expectedReceivedMessages)
                .After(host.AssertionTimeoutInMs)
                .MilliSeconds.PollEvery(milliSeconds: 10)
                .MilliSeconds
        );

        AssertReturnedResponses(returnedResponses2, testCase, host);

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

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var receivedMessages = new ConcurrentQueue<object>();
        var returnedResponses = new List<object>();

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (message, _, _) =>
            {
                receivedMessages.Enqueue(message);

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

        await using var senderHost = await host.CreateSenderTestHost(host.TestTimeoutToken);

        await testCase.BeforeSend(host);

        await Assert.ThatAsync(
            async () =>
                returnedResponses.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        AssertReceivedMessages(receivedMessages, testCase, host);
        AssertReturnedResponses(returnedResponses, testCase, host);

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
    public async Task GivenTestCase_WhenCancellingSending_CallerReceivesOperationCanceledExceptionAndReceiversReceiveNoMessages(
        TSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        var receivedMessages = new ConcurrentQueue<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (message, _, _) =>
            {
                receivedMessages.Enqueue(message);

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

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var senderHost = await host.CreateSenderTestHost(
            host.TestTimeoutToken,
            (_, _, ct) => tcs.Task.WaitAsync(ct)
        );

        await testCase.BeforeSend(host);

        using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var sendTask = testCase.SendMessages(senderHost.MessageSenders, sendCts.Token);

        await sendCts.CancelAsync();

        tcs.SetResult();

        await Assert.ThatAsync(
            () => sendTask.WaitAsync(host.AssertionTimeout, TimeProvider.System, host.TestTimeoutToken),
            Throws.InstanceOf<OperationCanceledException>()
        );

        await Task.Delay(host.ShortDelay, TimeProvider.System, host.TestTimeoutToken); // give any potential erroneous send operations time to complete

        Assert.That(receivedMessages, Is.Empty);

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

        var receivedMessages = new ConcurrentQueue<object>();
        var returnedResponses = new List<object>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);

        var messageTransportType = new MessageTransportType(
            TTestClass.TransportTypeName,
            MessageTransportRole.Receiver
        );

        if (testCase.ReceiverConfigurationException is { } configurationException)
        {
            await Assert.ThatAsync(
                () => host.CreateReceiverTestHost(cts.Token),
                Throws
                    .InstanceOf<MessageReceiverExecutionFailedException>()
                    .With.InnerException.SameAs(configurationException)
                    .And.Property(nameof(MessageReceiverExecutionFailedException.MessageTransportType))
                    .EqualTo(messageTransportType)
            );

            await testCase.OnReceiverConfigurationException(host);

            return;
        }

        host.Logger.LogInformation("Running receivers...");

        await using var receiverHost = await host.CreateReceiverTestHost(
            cts.Token,
            (message, _, _) =>
            {
                receivedMessages.Enqueue(message);

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
                    .InstanceOf<MessageReceiverExecutionFailedException>()
                    .With.Property(nameof(MessageReceiverExecutionFailedException.MessageTransportType))
                    .EqualTo(messageTransportType)
                    .Or.InstanceOf<AggregateException>()
                    .With.Property("InnerExceptions")
                    .Count.EqualTo(testCase.NumOfExpectedUnrecoverableConnectionErrors)
                    .With.Property("InnerExceptions")
                    .Matches<ReadOnlyCollection<Exception>>(exs =>
                        exs.All(ex =>
                            ex is MessageReceiverExecutionFailedException e
                            && e.MessageTransportType == messageTransportType
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
                    .InstanceOf<MessageReceiverExecutionFailedException>()
                    .With.Property(nameof(MessageReceiverExecutionFailedException.MessageTransportType))
                    .EqualTo(messageTransportType)
                    .Or.InstanceOf<AggregateException>()
                    .With.Property("InnerExceptions")
                    .Count.EqualTo(testCase.NumOfExpectedUnrecoverableConnectionErrors)
                    .With.Property("InnerExceptions")
                    .Matches<ReadOnlyCollection<Exception>>(exs =>
                        exs.All(ex =>
                            ex is MessageReceiverExecutionFailedException e
                            && e.MessageTransportType == messageTransportType
                        )
                    )
            );
        }

        await using var senderHost = await host.CreateSenderTestHost(host.TestTimeoutToken);

        host.Logger.LogInformation("Sending initial messages...");

        if (testCase.SendException is { } sendException)
        {
            // we expect the test case to internally handle that the send exception is thrown
            await Assert.ThatAsync(
                () => testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken),
                Throws.Exception.With.InnerException.SameAs(sendException)
            );

            await testCase.OnSendException(host);

            Assert.That(receivedMessages, Is.Empty);

            return;
        }

        // return if no active receivers are expected
        if (testCase.ExpectedReceivedMessages.Count is 0)
        {
            return;
        }

        if (receiverHost.ReceiverExecutionHandle is null)
        {
            // if the receiver is not using a handle, it means there is no point in testing
            // handler exceptions or trying to reconnect, since the lifetime of the receiver
            // is the same as the host
            return;
        }

        // at this point, all handlers should have connected successfully
        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle.InitialConnectionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ),
            Throws.Nothing
        );

        var handlerExceptions = testCase
            .HandlerExceptions.OfType<Exception>()
            .OrderBy(ex => ex.Message, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (handlerExceptions.Count > 0)
        {
            // when we expect a response, but the handler has an exception, then the send operation should time out
            if (testCase.ExpectedResponses.Count > 0)
            {
                using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
                sendCts.CancelAfter(host.ShortDelay);

                await Assert.ThatAsync(
                    () => testCase.SendMessages(senderHost.MessageSenders, sendCts.Token),
                    Throws
                        .InstanceOf<OperationCanceledException>()
                        .Or.InnerException.InstanceOf<OperationCanceledException>()
                );
            }
            else
            {
                await Assert.ThatAsync(
                    () => testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken),
                    Throws.Nothing
                );
            }

            await Assert.ThatAsync(
                () =>
                    receiverHost.ReceiverExecutionHandle.CompletionTask.WaitAsync(
                        host.AssertionTimeout,
                        TimeProvider.System,
                        cts.Token
                    ),
                Throws
                    .InstanceOf<MessageReceiverExecutionFailedException>()
                    .With.InnerException.SameAs(handlerExceptions[0])
                    .And.Property(nameof(MessageReceiverExecutionFailedException.MessageTransportType))
                    .EqualTo(messageTransportType)
                    .Or.InstanceOf<AggregateException>()
                    .With.Property("InnerExceptions")
                    .Count.EqualTo(handlerExceptions.Count)
                    .With.Property("InnerExceptions")
                    .Matches<ReadOnlyCollection<Exception>>(exs =>
                        exs.OfType<MessageReceiverExecutionFailedException>()
                            .Select(ex => ex.InnerException)
                            .OfType<Exception>()
                            .OrderBy(ex => ex.Message, StringComparer.OrdinalIgnoreCase)
                            .SequenceEqual(handlerExceptions)
                    )
            );

            AssertReceivedMessages(receivedMessages, testCase, host);

            await testCase.OnHandlerExceptions(host);

            // the initial connection task should not be affected by handler exceptions
            Assert.That(receiverHost.ReceiverExecutionHandle.InitialConnectionTask.IsCompletedSuccessfully, Is.True);

            return;
        }

        await Assert.ThatAsync(
            async () =>
                returnedResponses.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        AssertReturnedResponses(returnedResponses, testCase, host);

        host.Logger.LogInformation("Triggering reconnects...");

        await testCase.TriggerReconnect(host);

        // we expect reconnection to be immediate
        await testCase.AfterSuccessfulReconnect(host);

        host.Logger.LogInformation("Sending messages after reconnects...");

        receivedMessages.Clear();
        returnedResponses.Clear();

        await Assert.ThatAsync(
            async () =>
                returnedResponses.AddRange(
                    await testCase.SendMessages(senderHost.MessageSenders, host.TestTimeoutToken)
                ),
            Throws.Nothing
        );

        AssertReceivedMessages(receivedMessages, testCase, host);
        AssertReturnedResponses(returnedResponses, testCase, host);

        await cts.CancelAsync();

        await Assert.ThatAsync(
            () =>
                receiverHost.ReceiverExecutionHandle.CompletionTask.WaitAsync(
                    host.AssertionTimeout,
                    TimeProvider.System,
                    host.TestTimeoutToken
                ),
            Throws.Nothing
        );
    }

    [Test]
    public void GivenTransport_WhenGettingSuccessTestCases_AllRequiredTestCasesArePresent()
    {
        var testCases = TTestClass.CreateSuccessTestCases().ToList();

        var predicates = new List<
            Expression<Func<IMessageTransportConformityExecutionSuccessTestCase<TTestHost>, bool>>
        >
        {
            testCase => testCase.ShouldCompleteImmediately,
            testCase => testCase.MessagesAreSentInParallel,
            testCase => !testCase.MessagesAreSentInParallel,
            testCase => testCase.ExpectedReceivedMessages.Count == 0,
            testCase => testCase.ExpectedReceivedMessages.Count > 0,
            testCase => testCase.ExpectedReceivedMessages.Count > 1,
            testCase =>
                testCase.ExpectedReceivedMessages.Any(s =>
                    s.GetType()
                        .GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                        .Any(p =>
                            p.Name.EndsWith(".JsonSerializerContext", StringComparison.InvariantCulture)
                            && p.GetValue(s) != null
                        )
                ),
            testCase =>
                testCase.ExpectedReceivedMessages.Any(s =>
                    s.GetType()
                        .GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                        .Any(p =>
                            p.Name.EndsWith(".EmptyInstance", StringComparison.InvariantCulture)
                            && p.GetValue(s) != null
                        )
                ),
            testCase =>
                testCase.ExpectedReceivedMessages.Any(s =>
                    s.GetType().Name.EndsWith("ForAssemblyScanning", StringComparison.InvariantCulture)
                ),
            testCase =>
                testCase.ExpectedReceivedMessages.Any(s =>
                    s.GetType().Name.EndsWith("WithDelegateHandler", StringComparison.InvariantCulture)
                ),
            testCase =>
                testCase.ExpectedReceivedMessages.Any(s =>
                    s.GetType().BaseType != null
                    && s.GetType()
                        .BaseType!.GetCustomAttributes()
                        .Any(a =>
                            a.GetType().Name.Contains("MessageAttribute", StringComparison.InvariantCulture)
                            && !s.GetType().Name.Contains("WithoutResponse", StringComparison.InvariantCulture)
                        )
                ),
            testCase =>
                testCase.ExpectedReceivedMessages.Any(s =>
                    s.GetType().BaseType != null
                    && s.GetType()
                        .BaseType!.GetCustomAttributes()
                        .Any(a =>
                            a.GetType().Name.EndsWith("MessageAttribute", StringComparison.InvariantCulture)
                            && s.GetType().Name.Contains("WithoutResponse", StringComparison.InvariantCulture)
                        )
                ),
        };

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<IMessageTransportConformityExecutionSuccessTestCase<TTestHost>>(tc =>
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

        var predicates = new List<
            Expression<Func<IMessageTransportConformityExecutionSuccessTestCase<TTestHost>, bool>>
        >
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
                    Has.Some.Matches<IMessageTransportConformityExecutionSuccessTestCase<TTestHost>>(tc =>
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

        var predicates = new List<Expression<Func<IMessageTransportConformityExecutionErrorTestCase<TTestHost>, bool>>>
        {
            testCase => testCase.ReceiverConfigurationException != null,
            testCase => testCase.SendException != null,
        };

        if (TTestClass.TransportRequiresReceiverConnection)
        {
            predicates.AddRange(
                [
                    testCase => !testCase.HandlerExceptions.OfType<Exception>().Any(),
                    testCase => testCase.HandlerExceptions.OfType<Exception>().Take(2).Count() == 1,
                    testCase => testCase.HandlerExceptions.OfType<Exception>().Skip(1).Any(),
                    testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors == 0,
                    testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors == 1,
                    testCase => testCase.NumOfExpectedUnrecoverableConnectionErrors > 1,
                ]
            );
        }

        Assert.Multiple(() =>
        {
            foreach (var predicate in predicates)
            {
                Assert.That(
                    testCases,
                    Has.Some.Matches<IMessageTransportConformityExecutionErrorTestCase<TTestHost>>(tc =>
                        predicate.Compile().Invoke(tc)
                    ),
                    $"missing expected test case: {predicate.Body}"
                );
            }
        });
    }

    private static void AssertReceivedMessages(
        IReadOnlyCollection<object> receivedMessages,
        IMessageTransportConformityExecutionTestCase<TTestHost> testCase,
        IMessageTransportConformityTestHost host,
        bool allowOutOfOrder = false
    )
    {
        if (testCase.NumOfReceivers > 1 || testCase.MessagesAreSentInParallel || allowOutOfOrder)
        {
            // with multiple receivers or when sending messages in parallel, the order of messages is not
            // guaranteed, so we use EquivalentTo instead of EqualTo
            Assert.That(
                () => receivedMessages,
                Is.EquivalentTo(testCase.ExpectedReceivedMessages)
                    .After(host.AssertionTimeoutInMs)
                    .MilliSeconds.PollEvery(milliSeconds: 10)
                    .MilliSeconds
            );
        }
        else
        {
            Assert.That(
                () => receivedMessages,
                Is.EqualTo(testCase.ExpectedReceivedMessages)
                    .After(host.AssertionTimeoutInMs)
                    .MilliSeconds.PollEvery(milliSeconds: 10)
                    .MilliSeconds
            );
        }
    }

    private static void AssertReturnedResponses(
        IReadOnlyCollection<object> returnedResponses,
        IMessageTransportConformityExecutionTestCase<TTestHost> testCase,
        IMessageTransportConformityTestHost host
    )
    {
        if (testCase.MessagesAreSentInParallel)
        {
            // when sending messages in parallel, the order of responses is not guaranteed, so we use EquivalentTo instead of EqualTo
            Assert.That(
                () => returnedResponses,
                Is.EquivalentTo(testCase.ExpectedResponses)
                    .After(host.AssertionTimeoutInMs)
                    .MilliSeconds.PollEvery(milliSeconds: 10)
                    .MilliSeconds
            );
        }
        else
        {
            Assert.That(
                () => returnedResponses,
                Is.EqualTo(testCase.ExpectedResponses)
                    .After(host.AssertionTimeoutInMs)
                    .MilliSeconds.PollEvery(milliSeconds: 10)
                    .MilliSeconds
            );
        }
    }

    private static IEnumerable<TestCaseData> CreateSuccessTestCasesPrivate() =>
        TTestClass.CreateSuccessTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name));

    private static IEnumerable<TestCaseData> CreateTestCasesForConcurrentExecution() =>
        TTestClass.TransportSupportsConcurrentReceivers
            ? TTestClass.CreateSimpleSuccessTestCases().Select(tc => new TestCaseData(tc).SetName(tc.Name))
            : [];

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
