using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Benchmarks;

// ReSharper disable once UnreachableSwitchCaseDueToIntegerAnalysis

var toRun = 1;

switch (toRun)
{
    case 1:
        _ = BenchmarkRunner.Run(typeof(Program).Assembly);
        break;

    case 2:
        Run();
        break;
}

void Run()
{
    var sw = Stopwatch.StartNew();
    new MessageBenchmarks().RunWithConqueror(100_000, 16, 20);
    Console.WriteLine(sw.Elapsed);
}
