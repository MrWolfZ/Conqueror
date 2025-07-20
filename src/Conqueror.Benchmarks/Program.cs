using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Benchmarks;
using Conqueror.Benchmarks.Transports;

var toRun = args.Length > 0 ? args[0] : "message-bench";

Environment.SetEnvironmentVariable("CONQUEROR_INBOX_LOGGING", "true");

switch (toRun)
{
    case "message-bench":
        _ = BenchmarkRunner.Run<MessageBenchmarks>();

        break;

    case "message-manual":
    {
        var sw = Stopwatch.StartNew();
        new MessageBenchmarks().RunWithConquerorPreBuiltSender(numOfExecutions: 100_000, parallelism: 16, numOfMiddlewares: 20);
        Console.WriteLine(sw.Elapsed);

        break;
    }

    case "middleware-logging-bench":
        _ = BenchmarkRunner.Run<MessageLoggingMiddlewareBenchmarks>();

        break;

    case "middleware-logging-manual":
    {
        var sw = Stopwatch.StartNew();
        new MessageLoggingMiddlewareBenchmarks().Run(numOfExecutions: 100_000, parallelism: 16);
        Console.WriteLine(sw.Elapsed);

        break;
    }

    case "transport-file-system-message-bench":
        FileSystemMessageBenchmarks.Init();
        _ = BenchmarkRunner.Run<FileSystemMessageBenchmarks>();

        break;

    case "transport-file-system-message-manual":
    {
        FileSystemMessageBenchmarks.Init();

        var sw = Stopwatch.StartNew();
        new FileSystemMessageBenchmarks().Run(
            nrOfMessages: 1_000,
            nrOfSenders: 4,
            nrOfReceivers: 4,
            runSendersAndReceiversInSameProvider: false);

        Console.WriteLine(sw.Elapsed);

        break;
    }

    case "transport-file-system-message-no-response-manual":
    {
        FileSystemMessageBenchmarks.Init();

        var sw = Stopwatch.StartNew();
        new FileSystemMessageBenchmarks().RunWithoutResponse(
            nrOfMessages: 1_000,
            nrOfSenders: 4,
            nrOfReceivers: 4,
            runSendersAndReceiversInSameProvider: false);

        Console.WriteLine(sw.Elapsed);

        break;
    }

    case "transport-file-system-message-no-response-single-manual":
    {
        FileSystemMessageBenchmarks.Init();

        var sw = Stopwatch.StartNew();
        new FileSystemMessageBenchmarks().RunWithoutResponse(
            nrOfMessages: 1_000,
            nrOfSenders: 4,
            nrOfReceivers: 1,
            runSendersAndReceiversInSameProvider: false);

        Console.WriteLine(sw.Elapsed);

        break;
    }

    default:
        throw new ArgumentOutOfRangeException(nameof(toRun), toRun, $"unknown benchmark to run: {toRun}");
}
