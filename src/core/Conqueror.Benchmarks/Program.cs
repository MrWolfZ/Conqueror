using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Benchmarks;

var toRun = args.Length > 0 ? args[0] : "message-bench";

switch (toRun)
{
    case "message-bench":
        _ = BenchmarkRunner.Run<MessageBenchmarks>();

        break;

    case "message-manual":
    {
        var sw = Stopwatch.StartNew();
        new MessageBenchmarks().RunWithConquerorPreBuiltSender(
            numOfExecutions: 100_000,
            parallelism: 16,
            numOfMiddlewares: 20
        );
        Console.WriteLine(sw.Elapsed);

        break;
    }

    default:
        throw new ArgumentOutOfRangeException(nameof(args), toRun, $"unknown benchmark to run: {toRun}");
}
