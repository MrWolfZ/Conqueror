namespace Conqueror.Transport.FileSystem.Tests.Signalling;

using static FileSystemSignalTestCases;

[TestFixture]
[SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Members are used by ASP.NET Core via reflection")]
[SuppressMessage(
    "Structure",
    "NUnit1028:The non-test method is public",
    Justification = "test case generation methods must be public"
)]
public sealed partial class FileSystemSignalExecutionTests
{
    private readonly DirectoryInfo baseDirectory = FileSystemTestDirectory.Create();

    [Test]
    [Combinatorial]
    public void GivenMultipleFileSystemSignalTypesWithSameTag_WhenRunningReceivers_ThrowsException(
        [Values] bool runIndividually
    )
    {
        var clientServices = new ServiceCollection()
            .AddConquerorFileSystemTransport()
            .AddSignalHandler<TestSignalWithDuplicateTagHandler>()
            .AddSingleton(baseDirectory);

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        Assert.That(
            () =>
                runIndividually
                    ? signalReceivers.RunFileSystemSignalReceiver<TestSignalWithDuplicateTagHandler>(
                        CancellationToken.None
                    )
                    : signalReceivers.RunFileSystemSignalReceivers(CancellationToken.None),
            Throws
                .InstanceOf<SignalReceiverExecutionFailedException>()
                .With.InnerException.InstanceOf<InvalidOperationException>()
                .With.InnerException.Message.Contains("is already used by signal type")
        );
    }

    [Test]
    [TestCaseSource(typeof(FileSystemSignalTestCases), nameof(CreateSimpleSuccessTestCases))]
    [SuppressMessage(
        "Structure",
        "NUnit1018:The number of parameters provided by the TestCaseSource does not match the number of parameters in the target method",
        Justification = "false positive"
    )]
    public async Task GivenFileSystemSignalHandlerForMultipleSignalTypes_WhenRunningReceiver_OnlyConfiguresReceiverOnce(
        FileSystemSignalConformityExecutionSuccessTestCase testCase
    )
    {
        await using var host = testCase.CreateTestHost();

        await using var publisherHost = await host.CreatePublisherTestHost(host.TestTimeoutToken);

        var configCount = 0;

        var clientServices = new ServiceCollection()
            .AddConquerorFileSystemTransport()
            .AddSignalHandler<MultiTestSignalHandler>()
            .AddSingleton(baseDirectory)
            .AddSingleton<Action<IFileSystemSignalReceiver>>(r =>
            {
                configCount += 1;
                _ = r.EnableSingleInstance(
                    r.ServiceProvider.GetRequiredService<DirectoryInfo>().FullName,
                    TimeSpan.FromMilliseconds(value: 10)
                );
            });

        var clientServiceProvider = clientServices.BuildServiceProvider();

        var signalReceivers = clientServiceProvider.GetRequiredService<ISignalReceivers>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(host.TestTimeoutToken);
        await using var handle = signalReceivers.RunFileSystemSignalReceivers(cts.Token);

        await Assert.ThatAsync(
            () => handle.InitialConnectionTask.WaitAsync(host.AssertionTimeout, TimeProvider.System, cts.Token),
            Throws.Nothing
        );

        Assert.That(configCount, Is.EqualTo(expected: 1));
    }

    [Test]
    public async Task GivenMultipleCompetingHandlers_OneHandlerFailsProcessing_TheOtherHandlerProcessesTheSignal()
    {
        const string receiverName = "test-receiver";

        using var testTimeouts = FileSystemTransportTestTimeouts.Create();
        var leaseDuration = testTimeouts.TestTimeout;

        var receivedSignals = new ConcurrentQueue<TestSignal>();

        await using var host1 = await FileSystemTransportTestHost.Create(
            services =>
            {
                _ = services.AddFileSystemSignalHandlerDelegate(
                    TestSignal.T,
                    (_, _) => throw new InvalidOperationException("test exception"),
                    r =>
                        r.EnableMultipleCompetingInstances(
                                baseDirectory.FullName,
                                leaseDuration,
                                TimeSpan.FromMilliseconds(value: 10)
                            )
                            .WithName(receiverName)
                            .WithExceptionCallback(e =>
                                r.ServiceProvider.GetRequiredService<ILogger<FileSystemSignalExecutionTests>>()
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
                _ = services.AddFileSystemSignalHandlerDelegate(
                    TestSignal.T,
                    async (s, _, ct) =>
                    {
                        receivedSignals.Enqueue(s);

                        ct.ThrowIfCancellationRequested();

                        await Task.Yield();
                    },
                    r =>
                        r.EnableMultipleCompetingInstances(
                                baseDirectory.FullName,
                                leaseDuration,
                                TimeSpan.FromMilliseconds(value: 20)
                            ) // higher polling interval so that the other handler sees the message first
                            .WithName(receiverName)
                            .WithExceptionCallback(e =>
                                r.ServiceProvider.GetRequiredService<ILogger<FileSystemSignalExecutionTests>>()
                                    .LogError(e, "error occurred")
                            )
                );

                _ = services.AddConquerorFileSystemTransport();
            },
            testTimeouts.TestTimeoutToken
        );

        await using var handle1 = host1
            .Resolve<ISignalReceivers>()
            .RunFileSystemSignalReceivers(testTimeouts.TestTimeoutToken);
        await using var handle2 = host2
            .Resolve<ISignalReceivers>()
            .RunFileSystemSignalReceivers(testTimeouts.TestTimeoutToken);

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

        var publisher = senderServiceProvider
            .GetRequiredService<ISignalPublishers>()
            .For(TestSignal.T)
            .WithTransport(b => b.UseFileSystem(baseDirectory.FullName));

        var signal1 = new TestSignal { Payload = 10 };
        var signal2 = new TestSignal { Payload = 20 };

        await publisher.Handle(signal1, testTimeouts.TestTimeoutToken);
        await publisher.Handle(signal2, testTimeouts.TestTimeoutToken);

        Assert.That(
            () => receivedSignals,
            Is.EquivalentTo(new[] { signal1, signal2 })
                .After(testTimeouts.AssertionTimeoutInMs)
                .MilliSeconds.PollEvery(milliSeconds: 10)
                .MilliSeconds
        );
    }

    [FileSystemSignal(Tag = "duplicate-tag")]
    private sealed partial record TestSignalWithDuplicateTag1(int Payload);

    [FileSystemSignal(Tag = "duplicate-tag")]
    private sealed partial record TestSignalWithDuplicateTag2(int Payload);

    private sealed partial class TestSignalWithDuplicateTagHandler
        : TestSignalWithDuplicateTag1.IHandler,
            TestSignalWithDuplicateTag2.IHandler
    {
        static void IFileSystemSignalHandler.ConfigureFileSystemReceiver(IFileSystemSignalReceiver receiver) =>
            receiver.EnableSingleInstance(
                receiver.ServiceProvider.GetRequiredService<DirectoryInfo>().FullName,
                TimeSpan.FromMilliseconds(value: 10)
            );

        public Task Handle(TestSignalWithDuplicateTag1 signal, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        Task TestSignalWithDuplicateTag2.IHandler.Handle(
            TestSignalWithDuplicateTag2 signal,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
