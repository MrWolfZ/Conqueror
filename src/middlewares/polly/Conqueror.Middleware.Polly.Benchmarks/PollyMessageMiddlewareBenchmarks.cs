namespace Conqueror.Middleware.Polly.Benchmarks;

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Conqueror;
using global::Polly;
using Microsoft.Extensions.DependencyInjection;

[MemoryDiagnoser]
public partial class PollyMessageMiddlewareBenchmarks
{
    [Benchmark]
    [Arguments(10_000, 1)]
    [Arguments(10_000, 4)]
    [Arguments(10_000, 16)]
    public void Run(int numOfExecutions, int parallelism)
    {
        var serviceProvider = new ServiceCollection()
            .AddMessageHandler<TestMessageHandler>()
            .BuildServiceProvider();

        var handler = serviceProvider
            .GetRequiredService<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p => p.UsePolly(b => b.AddRetry(new() { MaxRetryAttempts = 3, Delay = TimeSpan.Zero })));

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await handler.Handle(new(idx), CancellationToken.None);

            if (response.Payload != idx + 1)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"got wrong result {response.Payload} on execution {idx}, expected {idx + 1}"
                    )
                );
            }
        }
    }

    [Benchmark]
    [Arguments(10_000, 1)]
    [Arguments(10_000, 4)]
    [Arguments(10_000, 16)]
    public void RunWithTimeout(int numOfExecutions, int parallelism)
    {
        var serviceProvider = new ServiceCollection()
            .AddMessageHandler<TestMessageHandler>()
            .BuildServiceProvider();

        var handler = serviceProvider
            .GetRequiredService<IMessageSenders>()
            .For(TestMessage.T)
            .WithPipeline(p => p.UsePolly(b => b
                .AddRetry(new() { MaxRetryAttempts = 3, Delay = TimeSpan.Zero })
                .AddTimeout(TimeSpan.FromSeconds(10))));

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await handler.Handle(new(idx), CancellationToken.None);

            if (response.Payload != idx + 1)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"got wrong result {response.Payload} on execution {idx}, expected {idx + 1}"
                    )
                );
            }
        }
    }

    [Benchmark]
    [Arguments(10_000, 1)]
    [Arguments(10_000, 4)]
    [Arguments(10_000, 16)]
    public void RunWithoutPolly(int numOfExecutions, int parallelism)
    {
        var serviceProvider = new ServiceCollection()
            .AddMessageHandler<TestMessageHandler>()
            .BuildServiceProvider();

        var handler = serviceProvider
            .GetRequiredService<IMessageSenders>()
            .For(TestMessage.T);

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await handler.Handle(new(idx), CancellationToken.None);

            if (response.Payload != idx + 1)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"got wrong result {response.Payload} on execution {idx}, expected {idx + 1}"
                    )
                );
            }
        }
    }

    private static async Task Run(Func<int, ValueTask> runSingle, int numOfExecutions, int? parallelism)
    {
        if (parallelism is not null)
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(start: 0, numOfExecutions),
                new ParallelOptions { MaxDegreeOfParallelism = parallelism.Value },
                (i, _) => runSingle(i)
            );

            return;
        }

        for (var i = 0; i < numOfExecutions; i += 1)
        {
            await runSingle(i);
        }
    }

    [Message<TestMessageResponse>]
    private sealed partial record TestMessage(int Payload);

    private sealed record TestMessageResponse(int Payload);

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            return new TestMessageResponse(message.Payload + 1);
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) { }
    }
}
