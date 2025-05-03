using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Benchmarks;

[Config(typeof(ConfigWithCustomEnvVars))]

// ReSharper disable once ClassCanBeSealed.Global (BenchmarkDotNet requires class to be unsealed)
public partial class MessageBenchmarks
{
    private readonly IServiceProvider serviceProvider = new ServiceCollection().AddMessageHandler<TestMessageHandler>()
                                                                               .BuildServiceProvider();

    [Params(
        null,
        0,
        100,
        1000,
        10000)]
    public static int? NumOfMiddlewares { get; set; }

    [Benchmark]
    public void RunMessageBenchmark()
    {
        if (NumOfMiddlewares is null)
        {
            var res = serviceProvider.GetRequiredService<TestMessageHandler>().Handle(new(0)).GetAwaiter().GetResult();

            if (res.Value != 0)
            {
                throw new InvalidOperationException($"got wrong result {res.Value}, expected 0");
            }

            return;
        }

        var result = serviceProvider.GetRequiredService<IMessageSenders>()
                                    .For(TestMessage.T)
                                    .Handle(new(0))
                                    .GetAwaiter()
                                    .GetResult();

        if (result.Value != NumOfMiddlewares)
        {
            throw new InvalidOperationException($"got wrong result {result.Value}, expected {NumOfMiddlewares}");
        }
    }

    // in case we want to test different implementations, we can toggle them with environment variables
    private sealed class ConfigWithCustomEnvVars : ManualConfig
    {
        // ReSharper disable once EmptyConstructor
        public ConfigWithCustomEnvVars()
        {
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
            for (var i = 0; i < NumOfMiddlewares; i++)
            {
                pipeline.Use(new TestMessageMiddleware<TestMessage, TestMessageResponse>());
            }
        }
    }

    private sealed class TestMessageMiddleware<TMessage, TResponse> : IMessageMiddleware<TMessage, TResponse>
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        public async Task<TResponse> Execute(MessageMiddlewareContext<TMessage, TResponse> ctx)
        {
            await Task.Yield();

            var q = (TestMessage)(object)ctx.Message;
            var newMessage = (TMessage)(object)new TestMessage(q.Value + 1);

            return await ctx.Next(newMessage, ctx.CancellationToken);
        }
    }
}
