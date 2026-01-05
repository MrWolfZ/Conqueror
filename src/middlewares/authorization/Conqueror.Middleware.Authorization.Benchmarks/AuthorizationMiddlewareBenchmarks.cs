namespace Conqueror.Middleware.Authorization.Benchmarks;

using System.Globalization;
using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Conqueror;
using Microsoft.Extensions.DependencyInjection;

[Config(typeof(ConfigWithCustomEnvVars))]
[MemoryDiagnoser]
public partial class AuthorizationMiddlewareBenchmarks
{
    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void Run(int numOfExecutions, int? parallelism)
    {
        var serviceProvider = new ServiceCollection()
            .AddMessageHandler<TestMessageHandler>()
            .BuildServiceProvider();

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();
            var identity = new ClaimsIdentity(authenticationType: "test");
            identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
            conquerorContext.CurrentPrincipal = new ClaimsPrincipal(identity);

            var response = await serviceProvider
                .GetRequiredService<IMessageSenders>()
                .For(TestMessage.T)
                .WithPipeline(static pipeline =>
                    pipeline.UseAuthorization(c =>
                        c.AddAuthorizationCheck("test", ctx =>
                            ctx.CurrentPrincipal?.Identity?.IsAuthenticated is true
                                ? ctx.Success()
                                : ctx.Unauthorized("Not authenticated")
                        )
                    )
                )
                .Handle(new(idx), CancellationToken.None);

            if (response.Value != idx)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"got wrong result {response.Value} on execution {idx}, expected {idx}"
                    )
                );
            }
        }
    }

    public static IEnumerable<object?[]> Arguments()
    {
        foreach (
            var (numOfExecutions, parallelism) in from numOfExecutions in new[] { 1, 100, 1_000, 10_000, 100_000 }
                                                  from parallelism in new int?[] { null, 4 }
                                                  where parallelism is null || numOfExecutions >= parallelism
                                                  select (numOfExecutions, parallelism)
        )
        {
            yield return [numOfExecutions, parallelism];
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

    private sealed class ConfigWithCustomEnvVars : ManualConfig
    {
        // ReSharper disable once EmptyConstructor
        public ConfigWithCustomEnvVars()
        {
            _ = AddJob(Job.ShortRun);
        }
    }

    [Message<TestMessageResponse>]
    private sealed partial record TestMessage(int Value);

    private sealed record TestMessageResponse(int Value);

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            return new TestMessageResponse(message.Value);
        }

        public static void ConfigurePipeline(TestMessage.IPipeline pipeline) =>
            pipeline.UseAuthorization(static c =>
                c.AddAuthorizationCheck("test", ctx =>
                    ctx.CurrentPrincipal?.Identity?.IsAuthenticated is true
                        ? ctx.Success()
                        : ctx.Unauthorized("Not authenticated")
                )
            );
    }
}
