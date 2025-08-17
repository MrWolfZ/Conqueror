using System.Diagnostics;
using System.Globalization;
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

    default:
        throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"unknown variant: {toRun}"));
}

static void Run()
{
    var sw = Stopwatch.StartNew();

    // new MessageBenchmarks().RunWithoutConqueror(10_000, 16, false);
    new MessageBenchmarks().RunWithConqueror(numOfExecutions: 10_000, parallelism: 16, enableLogging: false);
    Console.WriteLine(sw.Elapsed);
}
