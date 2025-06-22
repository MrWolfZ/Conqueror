using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conqueror.Benchmarks;

[Config(typeof(ConfigWithCustomEnvVars))]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Benchmark.NET requires non-sealed classes")]
public partial class MessageLoggingMiddlewareBenchmarks
{
    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void Run(int numOfExecutions, int? parallelism)
    {
        var serviceProvider = new ServiceCollection().AddMessageHandler<TestMessageHandler>()
                                                     .AddLogging(l => l.SetMinimumLevel(LogLevel.None))
                                                     .BuildServiceProvider();

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await serviceProvider.GetRequiredService<IMessageSenders>()
                                                .For(TestMessage.T)
                                                .WithPipeline(static pipeline => pipeline.UseLogging(c => c.StackTraceCaptureIsDisabled = true))
                                                .Handle(new(idx));

            if (response.Value != idx)
            {
                throw new InvalidOperationException($"got wrong result {response.Value} on execution {idx}, expected {idx}");
            }
        }
    }

    public static IEnumerable<object?[]> Arguments()
    {
        foreach (var (numOfExecutions, parallelism) in from numOfExecutions in new[] { 1, 100, 1_000, 10_000, 100_000 }
                                                       from parallelism in new int?[] { null, 4 }
                                                       where parallelism is null || numOfExecutions >= parallelism
                                                       select (numOfExecutions, parallelism))
        {
            yield return [numOfExecutions, parallelism];
        }
    }

    private static async Task Run(Func<int, ValueTask> runSingle, int numOfExecutions, int? parallelism)
    {
        if (parallelism is not null)
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, numOfExecutions),
                new ParallelOptions { MaxDegreeOfParallelism = parallelism.Value },
                (i, _) => runSingle(i));

            return;
        }

        for (var i = 0; i < numOfExecutions; i += 1)
        {
            await runSingle(i);
        }
    }

    // in case we want to test different implementations, we can toggle them with environment variables
    private sealed class ConfigWithCustomEnvVars : ManualConfig
    {
        // ReSharper disable once EmptyConstructor
        public ConfigWithCustomEnvVars()
        {
            AddJob(Job.ShortRun);

            // AddJob(Job.Default
            //           .WithEnvironmentVariables(new EnvironmentVariable("SOME_VAR", "SOME_VALUE"))
            //           .WithId("some ID"));
        }
    }

    [Message<TestMessageResponse>]
    private sealed partial record TestMessage(int Value);

    private sealed record TestMessageResponse(int Value);

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage query, CancellationToken cancellationToken = new())
        {
            await Task.Yield();

            return new(query.Value);
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) => pipeline.UseLogging(static c => c.StackTraceCaptureIsDisabled = true);
    }
}
