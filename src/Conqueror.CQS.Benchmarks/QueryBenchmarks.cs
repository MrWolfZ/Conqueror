using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Benchmarks;

// [Config(typeof(ConfigWithCustomEnvVars))]
// ReSharper disable once ClassCanBeSealed.Global (BenchmarkDotNet requires class to be unsealed)
public class QueryBenchmarks
{
    [Params(100, 1000, 10000)]
    public static int NumOfMiddlewares { get; set; }

    private readonly IServiceProvider provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                                                        .BuildServiceProvider();

    [Benchmark]
    public void ExecuteQueryWithMiddleware()
    {
        var result = provider.GetRequiredService<IQueryHandler<TestQuery, TestResponse>>()
                             .ExecuteQuery(new(0))
                             .GetAwaiter()
                             .GetResult();

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
            //           .WithEnvironmentVariables(new EnvironmentVariable(QueryPipelineStrategy, "RECURSIVE_INDEX"))
            //           .WithId("Recursive using index"));
            // AddJob(Job.Default
            //           .WithEnvironmentVariables(new EnvironmentVariable(QueryPipelineStrategy, "RECURSIVE_QUEUE"))
            //           .WithId("Recursive using queue"));
        }
    }

    private sealed record TestQuery(int Value);

    private sealed record TestResponse(int Value);

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestResponse>
    {
        public async Task<TestResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = new())
        {
            await Task.Yield();
            return new(query.Value);
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestResponse> pipeline)
        {
            for (var i = 0; i < NumOfMiddlewares; i++)
            {
                pipeline.Use(new TestQueryMiddleware<TestQuery, TestResponse>());
            }
        }
    }

    private sealed class TestQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();

            var newQuery = ctx.Query;

            if (ctx.Query is TestQuery q)
            {
                newQuery = (TQuery)(object)new TestQuery(q.Value + 1);
            }

            return await ctx.Next(newQuery, ctx.CancellationToken);
        }
    }
}
