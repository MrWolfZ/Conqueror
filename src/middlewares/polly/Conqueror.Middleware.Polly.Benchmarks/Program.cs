using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Middleware.Polly.Benchmarks;

var toRun = args.Length > 0 ? args[0] : "polly-bench";

switch (toRun)
{
    case "polly-bench":
        _ = BenchmarkRunner.Run<PollyMessageMiddlewareBenchmarks>();

        break;

    case "polly-manual":
    {
        var sw = Stopwatch.StartNew();
        new PollyMessageMiddlewareBenchmarks().Run(numOfExecutions: 100_000, parallelism: 16);
        Console.WriteLine(sw.Elapsed);

        break;
    }

    default:
        throw new ArgumentOutOfRangeException(nameof(args), toRun, $"unknown benchmark to run: {toRun}");
}
