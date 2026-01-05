using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Middleware.Logging.Benchmarks;

var toRun = args.Length > 0 ? args[0] : "middleware-logging-bench";

switch (toRun)
{
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

    default:
        throw new ArgumentOutOfRangeException(nameof(args), toRun, $"unknown benchmark to run: {toRun}");
}
