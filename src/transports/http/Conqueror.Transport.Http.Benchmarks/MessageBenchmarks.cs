namespace Conqueror.Transport.Http.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[Config(typeof(ConfigWithCustomEnvVars))]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Benchmark.NET requires non-sealed classes")]
internal sealed partial class MessageBenchmarks
{
    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void RunWithoutConqueror(int numOfExecutions, int? parallelism, bool enableLogging)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureLogging(logging => _ = enableLogging ? logging.AddSimpleConsole() : logging.ClearProviders())
            .UseEnvironment(Environments.Development)
            .ConfigureWebHost(webHost =>
                webHost
                    .UseTestServer()
                    .ConfigureServices(services => services.AddMessageHandler<TestMessageHandler>().AddRouting())
                    .Configure(app =>
                        app.UseRouting()
                            .UseEndpoints(e =>
                                e.MapPost(
                                    "/api",
                                    async (TestMessageHandler handler, [FromBody] TestMessage message) =>
                                        TypedResults.Ok(await handler.Handle(message, CancellationToken.None))
                                )
                            )
                    )
            );

        using var host = hostBuilder.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        var client = host.GetTestClient();

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var httpResponse = await client.PostAsJsonAsync("api", new TestMessage(idx), CancellationToken.None);
            _ = httpResponse.EnsureSuccessStatusCode();
            var response = await httpResponse.Content.ReadFromJsonAsync<TestMessageResponse>(CancellationToken.None);

            if (response?.Value != idx)
            {
                throw new InvalidOperationException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"got wrong result {response?.Value} on execution {idx}, expected {idx}"
                    )
                );
            }
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(Arguments))]
    public void RunWithConqueror(int numOfExecutions, int? parallelism, bool enableLogging)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureLogging(logging => _ = enableLogging ? logging.AddSimpleConsole() : logging.ClearProviders())
            .UseEnvironment(Environments.Development)
            .ConfigureWebHost(webHost =>
                webHost
                    .UseTestServer()
                    .ConfigureServices(services =>
                        services.AddMessageHandler<TestMessageHandler>().AddRouting().AddConquerorHttpServerAspNetCore()
                    )
                    .Configure(app =>
                        app.UseRouting().UseConquerorWellKnownErrorHandling().UseEndpoints(e => e.MapMessageEndpoints())
                    )
            );

        using var host = hostBuilder.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        var client = host.GetTestClient();

        var clientServiceProvider = new ServiceCollection().AddConquerorHttpClient().BuildServiceProvider();

        Run(RunSingle, numOfExecutions, parallelism).GetAwaiter().GetResult();

        async ValueTask RunSingle(int idx)
        {
            var response = await clientServiceProvider
                .GetRequiredService<IMessageSenders>()
                .For(TestMessage.T)
                .WithTransport(b => b.UseHttp(new("http://conqueror.test/")).WithHttpClient(client))
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
            var (numOfExecutions, parallelism) in from numOfExecutions in new[] { 1, 100, 1_000 }
            from parallelism in new int?[] { null, 4 }
            where parallelism is null || numOfExecutions >= parallelism
            select (numOfExecutions, parallelism)
        )
        {
            yield return [numOfExecutions, parallelism, false];
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

    // in case we want to test different implementations, we can toggle them with environment variables
    private sealed class ConfigWithCustomEnvVars : ManualConfig
    {
        // ReSharper disable once EmptyConstructor
        public ConfigWithCustomEnvVars()
        {
            _ = AddJob(Job.ShortRun);

            // AddJob(Job.Default
            //           .WithEnvironmentVariables(new EnvironmentVariable("SOME_VAR", "SOME_VALUE"))
            //           .WithId("some ID"));
        }
    }

    [HttpMessage<TestMessageResponse>]
    private sealed partial record TestMessage(int Value);

    private sealed record TestMessageResponse(int Value);

    private sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public async Task<TestMessageResponse> Handle(
            TestMessage message,
            CancellationToken cancellationToken = default
        )
        {
            await Task.Yield();

            return new TestMessageResponse(message.Value);
        }
    }
}
