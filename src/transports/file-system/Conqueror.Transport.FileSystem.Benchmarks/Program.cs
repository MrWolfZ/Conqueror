using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Transport.FileSystem.Benchmarks;

var toRun = args.Length > 0 ? args[0] : "file-system-message-bench";

Environment.SetEnvironmentVariable("CONQUEROR_INBOX_LOGGING", "true");

switch (toRun)
{
    case "file-system-message-bench":
        FileSystemMessageBenchmarks.Init();
        _ = BenchmarkRunner.Run<FileSystemMessageBenchmarks>();

        break;

    case "file-system-message-manual":
    {
        FileSystemMessageBenchmarks.Init();

        var sw = Stopwatch.StartNew();
        new FileSystemMessageBenchmarks().Run(
            numOfMessages: 1_000,
            numOfSenders: 4,
            numOfReceivers: 4,
            runSendersAndReceiversInSameProvider: false
        );

        Console.WriteLine(sw.Elapsed);

        break;
    }

    case "file-system-message-no-response-manual":
    {
        FileSystemMessageBenchmarks.Init();

        var sw = Stopwatch.StartNew();
        new FileSystemMessageBenchmarks().RunWithoutResponse(
            numOfMessages: 1_000,
            numOfSenders: 4,
            numOfReceivers: 4,
            runSendersAndReceiversInSameProvider: false
        );

        Console.WriteLine(sw.Elapsed);

        break;
    }

    case "file-system-message-no-response-single-manual":
    {
        FileSystemMessageBenchmarks.Init();

        var sw = Stopwatch.StartNew();
        new FileSystemMessageBenchmarks().RunWithoutResponse(
            numOfMessages: 1_000,
            numOfSenders: 4,
            numOfReceivers: 1,
            runSendersAndReceiversInSameProvider: false
        );

        Console.WriteLine(sw.Elapsed);

        break;
    }

    default:
        throw new ArgumentOutOfRangeException(nameof(args), toRun, $"unknown benchmark to run: {toRun}");
}
