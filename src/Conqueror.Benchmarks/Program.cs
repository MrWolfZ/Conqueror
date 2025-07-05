using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Benchmarks;

// ReSharper disable UnreachableSwitchCaseDueToIntegerAnalysis
// ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis

var toRun = 1;

switch (toRun)
{
    case 1:
        _ = BenchmarkRunner.Run<MessageBenchmarks>();

        break;

    case 2:
    {
        var sw = Stopwatch.StartNew();
        new MessageBenchmarks().RunWithConquerorPreBuiltSender(100_000, 16, 20);
        Console.WriteLine(sw.Elapsed);

        break;
    }

    case 3:
        _ = BenchmarkRunner.Run<MessageLoggingMiddlewareBenchmarks>();

        break;

    case 4:
    {
        var sw = Stopwatch.StartNew();
        new MessageLoggingMiddlewareBenchmarks().Run(100_000, 16);
        Console.WriteLine(sw.Elapsed);

        break;
    }
}
