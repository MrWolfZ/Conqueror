using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Benchmarks;

[Config(typeof(ConfigWithCustomEnvVars))]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Benchmark.NET requires non-sealed classes")]
public partial class MessageBenchmarks
{
    private readonly IServiceProvider serviceProvider = new ServiceCollection().AddMessageHandler<TestMessageHandler>()
                                                                               .BuildServiceProvider();

    [Benchmark]
    [ArgumentsSource(nameof(NoConquerorArguments))]
    public void RunWithoutConqueror(int numOfExecutions, int? parallelism)
    {
        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await serviceProvider.GetRequiredService<TestMessageHandler>()
                                                .Handle(new(idx));

            if (response.Value != idx)
            {
                throw new InvalidOperationException($"got wrong result {response.Value}, expected {idx}");
            }
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(ConquerorArguments))]
    public void RunWithConqueror(int numOfExecutions, int? parallelism, int numOfMiddlewares)
    {
        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await serviceProvider.GetRequiredService<IMessageSenders>()
                                                .For(TestMessage.T)
                                                .WithPipeline(pipeline =>
                                                {
                                                    for (var i = 0; i < numOfMiddlewares; i += 1)
                                                    {
                                                        pipeline.Use(
                                                            new TestMessageMiddleware<TestMessage, TestMessageResponse>
                                                                { Configuration = new() { Parameter = i } });
                                                    }

                                                    if (numOfMiddlewares > 0)
                                                    {
                                                        pipeline.Configure<TestMessageMiddleware<TestMessage, TestMessageResponse>>(static m => m.Configuration
                                                            .Parameter = 1);
                                                    }
                                                })
                                                .Handle(new(idx));

            if (response.Value != numOfMiddlewares + idx)
            {
                throw new InvalidOperationException($"got wrong result {response.Value}, expected {numOfMiddlewares + idx}");
            }
        }
    }

    public static IEnumerable<object?[]> NoConquerorArguments()
    {
        foreach (var (numOfExecutions, parallelism) in from numOfExecutions in new[] { 1, 100, 1_000 }
                                                       from parallelism in new int?[] { null, 4 }
                                                       where parallelism is null || numOfExecutions >= parallelism
                                                       select (numOfExecutions, parallelism))
        {
            yield return [numOfExecutions, parallelism];
        }
    }

    public static IEnumerable<object?[]> ConquerorArguments()
    {
        foreach (var (args, numOfMiddlewares) in from args in NoConquerorArguments()
                                                 from numOfMiddlewares in new[] { 0, 10, 100 }
                                                 select (args, numOfMiddlewares))
        {
            yield return [..args, numOfMiddlewares];
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
            AddJob(
                Job.ShortRun
                   .WithToolchain(InProcessEmitToolchain.Instance));

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

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline)
        {
            _ = pipeline;
        }
    }

    private sealed record TestMessageMiddlewareConfiguration
    {
        public required int Parameter { get; set; }
    }

    private sealed class TestMessageMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public required TestMessageMiddlewareConfiguration Configuration { get; init; }

        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();

            var q = (TestMessage)(object)ctx.Message;
            var newMessage = (TMessage)(object)new TestMessage(q.Value + Configuration.Parameter);

            return await ctx.Next(newMessage, ctx.CancellationToken);
        }
    }
}
