﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Benchmarks;

// [Config(typeof(ConfigWithCustomEnvVars))]
// ReSharper disable once ClassCanBeSealed.Global (BenchmarkDotNet requires class to be unsealed)
public class CommandBenchmarks
{
    [Params(100, 1000, 10000)]
    public static int NumOfMiddlewares { get; set; }

    private readonly ICommandHandler<TestCommand, TestResponse> handler = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                                                                                 .BuildServiceProvider()
                                                                                                 .GetRequiredService<ICommandHandler<TestCommand, TestResponse>>();

    [Benchmark]
    public void RunCommandBenchmark()
    {
        var result = handler.Handle(new(0)).GetAwaiter().GetResult();

        if (result.Value != NumOfMiddlewares)
        {
            throw new InvalidOperationException($"got wrong result {result.Value}, expected {NumOfMiddlewares}");
        }
    }

    private sealed class ConfigWithCustomEnvVars : ManualConfig
    {
        public ConfigWithCustomEnvVars()
        {
            // AddJob(Job.Default
            //           .WithEnvironmentVariables(new EnvironmentVariable(CommandPipelineStrategy, "RECURSIVE_INDEX"))
            //           .WithId("Recursive using index"));
            // AddJob(Job.Default
            //           .WithEnvironmentVariables(new EnvironmentVariable(CommandPipelineStrategy, "RECURSIVE_QUEUE"))
            //           .WithId("Recursive using queue"));
        }
    }

    private sealed record TestCommand(int Value);

    private sealed record TestResponse(int Value);

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestResponse>
    {
        public async Task<TestResponse> Handle(TestCommand query, CancellationToken cancellationToken = new())
        {
            await Task.Yield();
            return new(query.Value);
        }

        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestResponse> pipeline)
        {
            for (var i = 0; i < NumOfMiddlewares; i++)
            {
                pipeline.Use(new TestCommandMiddleware<TestCommand, TestResponse>());
            }
        }
    }

    private sealed class TestCommandMiddleware<TCommand, TResponse> : ICommandMiddleware<TCommand, TResponse>
        where TCommand : class
    {
        public async Task<TResponse> Execute(CommandMiddlewareContext<TCommand, TResponse> ctx)
        {
            await Task.Yield();

            var q = (TestCommand)(object)ctx.Command;
            var newCommand = (TCommand)(object)new TestCommand(q.Value + 1);

            return await ctx.Next(newCommand, ctx.CancellationToken);
        }
    }
}
