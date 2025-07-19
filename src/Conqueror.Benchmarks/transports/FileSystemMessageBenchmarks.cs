using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Benchmarks.Transports;

[Config(typeof(FileSystemBenchmarkConfig))]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Benchmark.NET requires non-sealed classes")]
public partial class FileSystemMessageBenchmarks
{
    private static readonly DirectoryInfo BenchmarkBaseDirectory = new(Path.Join(AppDomain.CurrentDomain.BaseDirectory, ".benchmarks"));

    public static void Init()
    {
        // recreate the base directory for each run
        if (BenchmarkBaseDirectory.Exists)
        {
            BenchmarkBaseDirectory.Delete(true);
        }

        BenchmarkBaseDirectory.Create();
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void Run(
        int nrOfMessages,
        int nrOfSenders,
        int nrOfReceivers,
        bool runSendersAndReceiversInSameProvider)
    {
        var dir = CreateDirectory();

        using var cts = new CancellationTokenSource();

        using var receiverProvider = new ServiceCollection().AddMessageHandler<TestMessageHandler>()
                                                            .AddConquerorFileSystemTransport()
                                                            .AddSingleton(new RunConfig(dir, nrOfReceivers == 1))
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
            var pollingInterval = TimeSpan.FromMilliseconds(100);
            var messageSenders = serviceProvider.GetRequiredService<IMessageSenders>();

            var senders = Enumerable.Range(0, nrOfSenders)
                                    .Select(_ => messageSenders.For(TestMessage.T)
                                                               .WithTransport(b => b.UseFileSystem(dir.FullName, pollingInterval)))
                                    .ToArray();

            if (senders.Length == 1)
            {
                await RunSender(senders[0], nrOfMessages, cancellationToken);

                return;
            }

            await Task.WhenAll(senders.Select(s => RunSender(s, nrOfMessages / senders.Length, cancellationToken)));
        }

        async Task RunReceivers(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var messageReceivers = serviceProvider.GetRequiredService<IMessageReceivers>();

            var handles = Enumerable.Range(0, nrOfReceivers)
                                    .Select(_ => messageReceivers.RunFileSystemMessageReceiver<TestMessageHandler>(cancellationToken))
                                    .ToArray();

            await using var combinedHandle = messageReceivers.CombineExecutions(handles);

            await combinedHandle.InitialConnectionTask;

            await combinedHandle.CompletionTask;
        }

        static async Task RunSender(TestMessage.IHandler sender, int nrOfMessages, CancellationToken cancellationToken)
        {
            for (var i = 0; i < nrOfMessages; i += 1)
            {
                var response = await sender.Handle(new(i), cancellationToken);

                if (response.Value != i + 1)
                {
                    throw new InvalidOperationException($"got wrong result, expected {i + 1} but got {response.Value}");
                }
            }
        }
    }

    public static IEnumerable<object?[]> Arguments()
    {
        foreach (var t in from nrOfMessages in new[] { 10, 100 }
                          from nrOfSenders in new[] { 1, 5 }
                          from nrOfReceivers in new[] { 1, 5 }
                          from runSendersAndReceiversInSameHost in new[] { true, false }
                          select (nrOfMessages, nrOfSenders, nrOfReceivers, runSendersAndReceiversInSameHost))
        {
            yield return [t.nrOfMessages, t.nrOfSenders, t.nrOfReceivers, t.runSendersAndReceiversInSameHost];
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
        public FileSystemBenchmarkConfig()
        {
            AddJob(Job.ShortRun);
        }
    }

    private sealed record RunConfig(DirectoryInfo BaseDirectory, bool IsSingleReceiver);

    [FileSystemMessage<TestMessageResponse>]
    private sealed partial record TestMessage(int Value);

    private sealed record TestMessageResponse(int Value);

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = new())
        {
            await Task.Delay(10, cancellationToken);

            return new(message.Value + 1);
        }

        public static void ConfigureFileSystemReceiver(IFileSystemMessageReceiver receiver)
        {
            var runConfig = receiver.ServiceProvider.GetRequiredService<RunConfig>();

            if (runConfig.IsSingleReceiver)
            {
                receiver.EnableSingleInstance(runConfig.BaseDirectory.FullName, pollingInterval: TimeSpan.FromMilliseconds(100));
            }
            else
            {
                receiver.EnableMultipleCompetingInstances(
                    runConfig.BaseDirectory.FullName,
                    leaseDuration: TimeSpan.FromMilliseconds(1_000),
                    pollingInterval: TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
