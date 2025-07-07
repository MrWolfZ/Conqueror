using static Conqueror.Transport.FileSystem.Tests.Messaging.FileSystemMessageTestCases;

namespace Conqueror.Transport.FileSystem.Tests.Messaging;

[TestFixture]
public sealed class FileSystemMessageExecutionTests : IDisposable
{
    private readonly DirectoryInfo baseDirectory = FileSystemTestDirectory.Create();

    [Test]
    public async Task GivenTestFileSystemMessageSentWithTimeToLive_WhenReceiverSeesMessageAfterExpiry_MessageIsNotProcessed()
    {
        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var pollingInterval = TimeSpan.FromMilliseconds(10);
        var timeToLive = TimeSpan.FromMilliseconds(10);

        await using var host = await FileSystemTransportTestHost.Create(services =>
        {
            _ = services.AddFileSystemMessageHandlerDelegate(
                TestMessage.T,
                async (m, _, ct) =>
                {
                    await Task.Delay(timeToLive * 2, ct);

                    return new() { Payload = m.Payload + 1 };
                },
                r => r.EnableSingleInstance(baseDirectory.FullName, pollingInterval)
                      .WithExceptionCallback(e => r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                                   .LogError(e, "error occurred")));

            _ = services.AddConquerorFileSystemTransport();
        });

        await using var handle = host.Resolve<IMessageReceivers>().RunFileSystemMessageReceivers(testTimeouts.TestTimeoutToken);

        await Assert.ThatAsync(
            () => handle.InitialConnectionTask.WaitAsync(testTimeouts.AssertionTimeout, testTimeouts.TestTimeoutToken),
            Throws.Nothing);

        await using var senderServiceProvider = new ServiceCollection().AddConquerorFileSystemTransport().BuildServiceProvider();

        var sender = senderServiceProvider.GetRequiredService<IMessageSenders>()
                                          .For(TestMessage.T)
                                          .WithTransport(b => b.UseFileSystem(baseDirectory.FullName, pollingInterval).WithTimeToLive(timeToLive));

        // the second message should be seen after its TTL, so the receiver ignores it and the sender never gets a response
        await Assert.ThatAsync(
            () => Task.WhenAll(
                          sender.Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken),
                          sender.Handle(new() { Payload = 20 }, testTimeouts.TestTimeoutToken))
                      .WaitAsync(testTimeouts.AssertionTimeout, testTimeouts.TestTimeoutToken),
            Throws.TypeOf<TimeoutException>());
    }

    [Test]
    public async Task GivenMultipleCompetingHandlers_OneHandlerFailsProcessing_TheOtherHandlerProcessesTheMessage()
    {
        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var leaseDuration = testTimeouts.TestTimeout;

        await using var host1 = await FileSystemTransportTestHost.Create(services =>
        {
            _ = services.AddFileSystemMessageHandlerDelegate(
                TestMessage.T,
                (_, _) => throw new InvalidOperationException("test exception"),
                r => r.EnableMultipleCompetingInstances(baseDirectory.FullName, leaseDuration, pollingInterval: TimeSpan.FromMilliseconds(10))
                      .WithExceptionCallback(e => r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                                   .LogError(e, "error occurred")));

            _ = services.AddConquerorFileSystemTransport();
        });

        await using var host2 = await FileSystemTransportTestHost.Create(services =>
        {
            _ = services.AddFileSystemMessageHandlerDelegate(
                TestMessage.T,
                async (m, _, ct) =>
                {
                    ct.ThrowIfCancellationRequested();

                    await Task.Yield();

                    return new() { Payload = m.Payload + 1 };
                },
                r => r
                     .EnableMultipleCompetingInstances(
                         baseDirectory.FullName,
                         leaseDuration,
                         pollingInterval: TimeSpan.FromMilliseconds(20)) // higher polling interval so that the other handler sees the message first
                     .WithExceptionCallback(e => r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                                  .LogError(e, "error occurred")));

            _ = services.AddConquerorFileSystemTransport();
        });

        await using var handle1 = host1.Resolve<IMessageReceivers>().RunFileSystemMessageReceivers(testTimeouts.TestTimeoutToken);
        await using var handle2 = host2.Resolve<IMessageReceivers>().RunFileSystemMessageReceivers(testTimeouts.TestTimeoutToken);

        await Assert.ThatAsync(
            () => handle1.InitialConnectionTask.WaitAsync(testTimeouts.AssertionTimeout, testTimeouts.TestTimeoutToken),
            Throws.Nothing);

        await Assert.ThatAsync(
            () => handle2.InitialConnectionTask.WaitAsync(testTimeouts.AssertionTimeout, testTimeouts.TestTimeoutToken),
            Throws.Nothing);

        await using var senderServiceProvider = new ServiceCollection().AddConquerorFileSystemTransport().BuildServiceProvider();

        var sender = senderServiceProvider.GetRequiredService<IMessageSenders>()
                                          .For(TestMessage.T)
                                          .WithTransport(b => b.UseFileSystem(baseDirectory.FullName, pollingInterval: TimeSpan.FromMilliseconds(10)));

        var response = await sender.Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken);

        Assert.That(response.Payload, Is.EqualTo(11));
    }

    [Test]
    public async Task GivenTestFileSystemMessageSentWithRetryLimit_WhenLimitIsExceeded_MessageIsNotProcessedAnymore()
    {
        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var assertionTimeout = testTimeouts.AssertionTimeout;
        var pollingInterval = TimeSpan.FromMilliseconds(10);

        var nrOfProcessingAttempts = 0;

        await using var senderServiceProvider = new ServiceCollection().AddConquerorFileSystemTransport().BuildServiceProvider();

        var sender = senderServiceProvider.GetRequiredService<IMessageSenders>()
                                          .For(TestMessage.T)
                                          .WithTransport(b => b.UseFileSystem(baseDirectory.FullName, pollingInterval));

        var sendTask = sender.Handle(new() { Payload = 10 }, testTimeouts.TestTimeoutToken);

        using var receiverCts = CancellationTokenSource.CreateLinkedTokenSource(testTimeouts.TestTimeoutToken);

        var receiversTask = RunReceivers(receiverCts.Token);

        try
        {
            await Assert.ThatAsync(() => sendTask.WaitAsync(assertionTimeout, testTimeouts.TestTimeoutToken), Throws.TypeOf<TimeoutException>());
        }
        finally
        {
            await receiverCts.CancelAsync();

            await Assert.ThatAsync(() => receiversTask.WaitAsync(assertionTimeout, testTimeouts.TestTimeoutToken), Throws.Nothing);

            Assert.That(nrOfProcessingAttempts, Is.EqualTo(3));
        }

        async Task RunReceivers(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await using var host = await FileSystemTransportTestHost.Create(services =>
                {
                    _ = services.AddFileSystemMessageHandlerDelegate(
                        TestMessage.T,
                        (_, _) =>
                        {
                            nrOfProcessingAttempts += 1;

                            throw new InvalidOperationException("test exception");
                        },
                        r => r.EnableSingleInstance(baseDirectory.FullName, pollingInterval)
                              .WithLimitNrOfFailedProcessingAttempts(3)
                              .WithMessageCallback(m => r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                                         .LogDebug("message callback: {Message}", m))
                              .WithExceptionCallback(e => r.ServiceProvider.GetRequiredService<ILogger<FileSystemMessageExecutionTests>>()
                                                           .LogError(e, "error occurred")));

                    _ = services.AddConquerorFileSystemTransport();
                });

                await using var handle = host.Resolve<IMessageReceivers>().RunFileSystemMessageReceivers(ct);

                await Assert.ThatAsync(
                    () => handle.InitialConnectionTask.WaitAsync(assertionTimeout, ct),
                    Throws.Nothing);

                await Assert.ThatAsync(
                    () => handle.CompletionTask.WaitAsync(assertionTimeout, ct),
                    Throws.TypeOf<MessageReceiverExecutionFailedException>().With.InnerException.InstanceOf<InvalidOperationException>()
                          .Or.InstanceOf<OperationCanceledException>());
            }
        }
    }

    public void Dispose()
    {
        if (baseDirectory.Exists)
        {
            baseDirectory.Delete(true);
        }
    }
}
