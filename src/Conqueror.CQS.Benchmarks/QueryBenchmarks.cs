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

    private readonly IQueryHandler<TestQuery, TestResponse> handler;

    public QueryBenchmarks()
    {
        handler = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                         .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                                         .BuildServiceProvider()
                                         .GetRequiredService<IQueryHandler<TestQuery, TestResponse>>();
    }

    [Benchmark]
    public void ExecuteQuery()
    {
        var result = handler.ExecuteQuery(new(0)).GetAwaiter().GetResult();

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
                pipeline.Use<TestQueryMiddleware>();
            }
        }
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();

            var q = (TestQuery)(object)ctx.Query;
            var newQuery = (TQuery)(object)new TestQuery(q.Value + 1);

            return await ctx.Next(newQuery, ctx.CancellationToken);
        }
    }
}
