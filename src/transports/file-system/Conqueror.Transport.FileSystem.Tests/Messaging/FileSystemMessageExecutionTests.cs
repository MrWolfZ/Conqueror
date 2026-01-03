namespace Conqueror.Transport.FileSystem.Tests.Messaging;

using static FileSystemMessageTestCases;

[TestFixture]
public sealed class FileSystemMessageExecutionTests : IDisposable
{
    private readonly DirectoryInfo baseDirectory = FileSystemTestDirectory.Create();

    [Test]
    public async Task GivenTestFileSystemMessageSentWithTimeToLive_WhenReceiverSeesMessageAfterExpiry_MessageIsNotProcessed()
    {
        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var pollingInterval = TimeSpan.FromMilliseconds(value: 10);
        var timeToLive = TimeSpan.FromMilliseconds(value: 10);

        await using var host = await FileSystemTransportTestHost.Create(
            services =>
            {
                _ = services.AddFileSystemMessageHandlerDelegate(
                    TestMessage.T,
                    async (m, _, ct) =>
                    {
                        await Task.Delay(timeToLive * 2, TimeProvider.System, ct);

                        return new() { Payload = m.Payload + 1 };
                    },
                    r =>
                        r.EnableSingleInstance(baseDirectory.FullName, pollingInterval)
                            .WithExceptionCallback(e =>
                                r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                    .LogError(e, "error occurred")
                            )
                );

                _ = services.AddConquerorFileSystemTransport();
            },
            testTimeouts.TestTimeoutToken
        );

        await using var handle = host.Resolve<IMessageReceivers>()
            .RunFileSystemMessageReceivers(testTimeouts.TestTimeoutToken);

        await Assert.ThatAsync(
            () =>
                handle.InitialConnectionTask.WaitAsync(
                    testTimeouts.AssertionTimeout,
                    TimeProvider.System,
                    testTimeouts.TestTimeoutToken
                ),
            Throws.Nothing
        );

        await using var senderServiceProvider = new ServiceCollection()
            .AddConquerorFileSystemTransport()
            .BuildServiceProvider();

        var sender = senderServiceProvider
            .GetRequiredService<IMessageSenders>()
            .For(TestMessage.T)
            .WithTransport(b => b.UseFileSystem(baseDirectory.FullName, pollingInterval).WithTimeToLive(timeToLive));

        // the second message should be seen after its TTL, so the receiver ignores it and the sender never gets a response
        await Assert.ThatAsync(
            () =>
                Task.WhenAll(
                        sender.Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken),
                        sender.Handle(new() { Payload = 20 }, testTimeouts.TestTimeoutToken)
                    )
                    .WaitAsync(testTimeouts.AssertionTimeout, TimeProvider.System, testTimeouts.TestTimeoutToken),
            Throws.TypeOf<TimeoutException>()
        );
    }

    [Test]
    public async Task GivenMultipleCompetingHandlers_WhenOneHandlerFailsProcessing_ThenTheOtherHandlerProcessesTheMessage()
    {
        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var leaseDuration = testTimeouts.TestTimeout;

        await using var host1 = await FileSystemTransportTestHost.Create(
            services =>
            {
                _ = services.AddFileSystemMessageHandlerDelegate(
                    TestMessage.T,
                    (_, _) => throw new InvalidOperationException("test exception"),
                    r =>
                        r.EnableMultipleCompetingInstances(
                                baseDirectory.FullName,
                                leaseDuration,
                                TimeSpan.FromMilliseconds(value: 10)
                            )
                            .WithExceptionCallback(e =>
                                r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                    .LogError(e, "error occurred")
                            )
                );

                _ = services.AddConquerorFileSystemTransport();
            },
            testTimeouts.TestTimeoutToken
        );

        await using var host2 = await FileSystemTransportTestHost.Create(
            services =>
            {
                _ = services.AddFileSystemMessageHandlerDelegate(
                    TestMessage.T,
                    async (m, _, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();

                        await Task.Yield();

                        return new() { Payload = m.Payload + 1 };
                    },
                    r =>
                        r.EnableMultipleCompetingInstances(
                                baseDirectory.FullName,
                                leaseDuration,
                                TimeSpan.FromMilliseconds(value: 20)
                            ) // higher polling interval so that the other handler sees the message first
                            .WithExceptionCallback(e =>
                                r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                    .LogError(e, "error occurred")
                            )
                );

                _ = services.AddConquerorFileSystemTransport();
            },
            testTimeouts.TestTimeoutToken
        );

        await using var handle1 = host1
            .Resolve<IMessageReceivers>()
            .RunFileSystemMessageReceivers(testTimeouts.TestTimeoutToken);
        await using var handle2 = host2
            .Resolve<IMessageReceivers>()
            .RunFileSystemMessageReceivers(testTimeouts.TestTimeoutToken);

        await Assert.ThatAsync(
            () =>
                handle1.InitialConnectionTask.WaitAsync(
                    testTimeouts.AssertionTimeout,
                    TimeProvider.System,
                    testTimeouts.TestTimeoutToken
                ),
            Throws.Nothing
        );

        await Assert.ThatAsync(
            () =>
                handle2.InitialConnectionTask.WaitAsync(
                    testTimeouts.AssertionTimeout,
                    TimeProvider.System,
                    testTimeouts.TestTimeoutToken
                ),
            Throws.Nothing
        );

        await using var senderServiceProvider = new ServiceCollection()
            .AddConquerorFileSystemTransport()
            .BuildServiceProvider();

        var sender = senderServiceProvider
            .GetRequiredService<IMessageSenders>()
            .For(TestMessage.T)
            .WithTransport(b => b.UseFileSystem(baseDirectory.FullName, TimeSpan.FromMilliseconds(value: 10)));

        var response = await sender.Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken);

        Assert.That(response.Payload, Is.EqualTo(expected: 11));
    }

    [Test]
    public async Task GivenTestFileSystemMessageSentWithRetryLimit_WhenLimitIsExceeded_MessageIsNotProcessedAnymore()
    {
        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var assertionTimeout = testTimeouts.AssertionTimeout;
        var testTimeoutToken = testTimeouts.TestTimeoutToken;
        var pollingInterval = TimeSpan.FromMilliseconds(value: 10);

        var numOfProcessingAttempts = 0;

        await using var senderServiceProvider = new ServiceCollection()
            .AddConquerorFileSystemTransport()
            .BuildServiceProvider();

        var sender = senderServiceProvider
            .GetRequiredService<IMessageSenders>()
            .For(TestMessage.T)
            .WithTransport(b => b.UseFileSystem(baseDirectory.FullName, pollingInterval));

        var sendTask = sender.Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken);

        using var receiverCts = CancellationTokenSource.CreateLinkedTokenSource(testTimeouts.TestTimeoutToken);

        var receiversTask = RunReceivers(receiverCts.Token);

        try
        {
            await Assert.ThatAsync(
                () => sendTask.WaitAsync(assertionTimeout, TimeProvider.System, testTimeouts.TestTimeoutToken),
                Throws.TypeOf<TimeoutException>()
            );
        }
        finally
        {
            await receiverCts.CancelAsync();

            await Assert.ThatAsync(
                () => receiversTask.WaitAsync(assertionTimeout, TimeProvider.System, testTimeouts.TestTimeoutToken),
                Throws.Nothing
            );

            Assert.That(numOfProcessingAttempts, Is.EqualTo(expected: 3));
        }

        async Task RunReceivers(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await using var host = await FileSystemTransportTestHost.Create(
                    services =>
                    {
                        _ = services.AddFileSystemMessageHandlerDelegate(
                            TestMessage.T,
                            (_, _) =>
                            {
                                numOfProcessingAttempts += 1;

                                throw new InvalidOperationException("test exception");
                            },
                            r =>
                                r.EnableSingleInstance(baseDirectory.FullName, pollingInterval)
                                    .WithLimitNrOfFailedProcessingAttempts(limitNrOfFailedProcessingAttempts: 3)
                                    .WithMessageCallback(m =>
                                        r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                            .LogDebug("message callback: {Message}", m)
                                    )
                                    .WithExceptionCallback(e =>
                                        r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                            .LogError(e, "error occurred")
                                    )
                        );

                        _ = services.AddConquerorFileSystemTransport();
                    },
                    testTimeoutToken
                );

                await using var handle = host.Resolve<IMessageReceivers>().RunFileSystemMessageReceivers(ct);

                await Assert.ThatAsync(
                    () => handle.InitialConnectionTask.WaitAsync(assertionTimeout, TimeProvider.System, ct),
                    Throws.Nothing
                );

                await Assert.ThatAsync(
                    () => handle.CompletionTask.WaitAsync(assertionTimeout, TimeProvider.System, ct),
                    Throws
                        .TypeOf<MessageReceiverExecutionFailedException>()
                        .With.InnerException.InstanceOf<InvalidOperationException>()
                        .Or.InstanceOf<OperationCanceledException>()
                );
            }
        }
    }

    public void Dispose()
    {
        if (baseDirectory.Exists)
        {
            baseDirectory.Delete(recursive: true);
        }
    }
}
