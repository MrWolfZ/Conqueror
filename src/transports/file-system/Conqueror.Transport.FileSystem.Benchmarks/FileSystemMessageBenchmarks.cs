namespace Conqueror.Transport.FileSystem.Benchmarks;

using System.Globalization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;

[Config(typeof(FileSystemBenchmarkConfig))]
[MemoryDiagnoser]
public partial class FileSystemMessageBenchmarks
{
    private static readonly DirectoryInfo BenchmarkBaseDirectory = new(
        Path.Join(AppDomain.CurrentDomain.BaseDirectory, ".benchmarks")
    );

    public static void Init()
    {
        // recreate the base directory for each run
        if (BenchmarkBaseDirectory.Exists)
        {
            BenchmarkBaseDirectory.Delete(recursive: true);
        }

        BenchmarkBaseDirectory.Create();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void Run(int numOfMessages, int numOfSenders, int numOfReceivers, bool runSendersAndReceiversInSameProvider)
    {
        var dir = CreateDirectory();

        using var cts = new CancellationTokenSource();

        using var receiverProvider = new ServiceCollection()
            .AddMessageHandler<TestMessageHandler>()
            .AddConquerorFileSystemTransport()
            .AddSingleton(new RunConfig(dir, numOfReceivers is 1))
            .BuildServiceProvider();

        using var senderProvider = runSendersAndReceiversInSameProvider
            ? receiverProvider
            : new ServiceCollection().AddConquerorFileSystemTransport().BuildServiceProvider();

        var sendersTask = RunSenders(senderProvider, cts.Token);
        var receiversTask = RunReceivers(receiverProvider, cts.Token);

        sendersTask.GetAwaiter().GetResult();

        cts.Cancel();

        receiversTask.GetAwaiter().GetResult();

        async Task RunSenders(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var pollingInterval = TimeSpan.FromMilliseconds(value: 100);
            var messageSenders = serviceProvider.GetRequiredService<IMessageSenders>();

            var senders = Enumerable
                .Range(start: 0, numOfSenders)
                .Select(_ =>
                    messageSenders.For(TestMessage.T).WithTransport(b => b.UseFileSystem(dir.FullName, pollingInterval))
                )
                .ToArray();

            if (senders.Length is 1)
            {
                await RunSender(senders[0], numOfMessages, cancellationToken);

                return;
            }

            await Task.WhenAll(senders.Select(s => RunSender(s, numOfMessages / senders.Length, cancellationToken)));
        }

        async Task RunReceivers(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var messageReceivers = serviceProvider.GetRequiredService<IMessageReceivers>();

            var handles = Enumerable
                .Range(start: 0, numOfReceivers)
                .Select(_ => messageReceivers.RunFileSystemMessageReceiver<TestMessageHandler>(cancellationToken))
                .ToArray();

            await using var combinedHandle = messageReceivers.CombineExecutions(handles);

            await combinedHandle.InitialConnectionTask;

            await combinedHandle.CompletionTask;
        }

        static async Task RunSender(TestMessage.IHandler sender, int numOfMessages, CancellationToken cancellationToken)
        {
            for (var i = 0; i < numOfMessages; i += 1)
            {
                var response = await sender.Handle(new(i), cancellationToken);

                if (response.Value != i + 1)
                {
                    throw new InvalidOperationException(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"got wrong result, expected {i + 1} but got {response.Value}"
                        )
                    );
                }
            }
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void RunWithoutResponse(
        int numOfMessages,
        int numOfSenders,
        int numOfReceivers,
        bool runSendersAndReceiversInSameProvider
    )
    {
        var dir = CreateDirectory();

        using var cts = new CancellationTokenSource();

        var withoutResponseResult = new WithoutResponseResult(numOfMessages);
        using var receiverProvider = new ServiceCollection()
            .AddMessageHandler<TestMessageHandler>()
            .AddConquerorFileSystemTransport()
            .AddSingleton(new RunConfig(dir, numOfReceivers is 1))
            .AddSingleton(withoutResponseResult)
            .BuildServiceProvider();

        using var senderProvider = runSendersAndReceiversInSameProvider
            ? receiverProvider
            : new ServiceCollection().AddConquerorFileSystemTransport().BuildServiceProvider();

        var sendersTask = RunSenders(senderProvider, cts.Token);
        var receiversTask = RunReceivers(receiverProvider, cts.Token);

        sendersTask.GetAwaiter().GetResult();
        withoutResponseResult.CompletionTask.GetAwaiter().GetResult();

        cts.Cancel();

        receiversTask.GetAwaiter().GetResult();

        async Task RunSenders(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var pollingInterval = TimeSpan.FromMilliseconds(value: 100);
            var messageSenders = serviceProvider.GetRequiredService<IMessageSenders>();

            var senders = Enumerable
                .Range(start: 0, numOfSenders)
                .Select(_ =>
                    messageSenders
                        .For(TestMessageWithoutResponse.T)
                        .WithTransport(b => b.UseFileSystem(dir.FullName, pollingInterval))
                )
                .ToArray();

            if (senders.Length is 1)
            {
                await RunSender(senders[0], numOfMessages, cancellationToken);

                return;
            }

            await Task.WhenAll(senders.Select(s => RunSender(s, numOfMessages / senders.Length, cancellationToken)));
        }

        async Task RunReceivers(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var messageReceivers = serviceProvider.GetRequiredService<IMessageReceivers>();

            var handles = Enumerable
                .Range(start: 0, numOfReceivers)
                .Select(_ => messageReceivers.RunFileSystemMessageReceiver<TestMessageHandler>(cancellationToken))
                .ToArray();

            await using var combinedHandle = messageReceivers.CombineExecutions(handles);

            await combinedHandle.InitialConnectionTask;

            await combinedHandle.CompletionTask;
        }

        static async Task RunSender(
            TestMessageWithoutResponse.IHandler sender,
            int numOfMessages,
            CancellationToken cancellationToken
        )
        {
            for (var i = 0; i < numOfMessages; i += 1)
            {
                await sender.Handle(new(i), cancellationToken);
            }
        }
    }

    public static IEnumerable<object?[]> Arguments()
    {
        foreach (
            var t in from numOfMessages in new[] { 10, 100 }
                     from numOfSenders in new[] { 1, 5 }
                     from numOfReceivers in new[] { 1, 5 }
                     from runSendersAndReceiversInSameHost in new[] { true, false }
                     select (numOfMessages, numOfSenders, numOfReceivers, runSendersAndReceiversInSameHost)
        )
        {
            yield return [t.numOfMessages, t.numOfSenders, t.numOfReceivers, t.runSendersAndReceiversInSameHost];
        }
    }

    private static DirectoryInfo CreateDirectory()
    {
        var benchmarkRunDir = Path.Join(BenchmarkBaseDirectory.FullName, Guid.NewGuid().ToString());
        var dirInfo = new DirectoryInfo(benchmarkRunDir);

        dirInfo.Create();

        return dirInfo;
    }

    private sealed class FileSystemBenchmarkConfig : ManualConfig
    {
        public FileSystemBenchmarkConfig() => AddJob(Job.ShortRun);
    }

    private sealed record RunConfig(DirectoryInfo BaseDirectory, bool IsSingleReceiver);

    private sealed class WithoutResponseResult(int numOfMessages)
    {
        private readonly TaskCompletionSource tcs = new();

        private int numOfMessagesReceived;

        public Task CompletionTask => tcs.Task;

        public void Signal()
        {
            if (Interlocked.Increment(ref numOfMessagesReceived) == numOfMessages)
            {
                tcs.SetResult();
            }
        }
    }

    [FileSystemMessage<TestMessageResponse>]
    private sealed partial record TestMessage(int Value);

    [FileSystemMessage]
    private sealed partial record TestMessageWithoutResponse(int Value);

    private sealed record TestMessageResponse(int Value);

    private sealed partial class TestMessageHandler(WithoutResponseResult? withoutResponseResult = null)
        : TestMessage.IHandler,
            TestMessageWithoutResponse.IHandler
    {
        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
        {
            var runConfig = receiver.ServiceProvider.GetRequiredService<RunConfig>();

            if (runConfig.IsSingleReceiver)
            {
                _ = receiver.EnableSingleInstance(
                    runConfig.BaseDirectory.FullName,
                    TimeSpan.FromMilliseconds(value: 100)
                );
            }
            else
            {
                _ = receiver.EnableMultipleCompetingInstances(
                    runConfig.BaseDirectory.FullName,
                    TimeSpan.FromMilliseconds(value: 1_000),
                    TimeSpan.FromMilliseconds(value: 100)
                );
            }
        }

        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Delay(millisecondsDelay: 10, cancellationToken);

            return new TestMessageResponse(message.Value + 1);
        }

        public async Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
        {
            await Task.Delay(millisecondsDelay: 10, cancellationToken);

            withoutResponseResult?.Signal();
        }
    }
}
