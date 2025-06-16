using System.Diagnostics;
using BenchmarkDotNet.Running;
using Conqueror.Transport.Http.Benchmarks;

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
    // new MessageBenchmarks().RunWithoutConqueror(10_000, 16, false);
    new MessageBenchmarks().RunWithConqueror(10_000, 16, false);
    Console.WriteLine(sw.Elapsed);
}
