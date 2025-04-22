namespace Conqueror.Tests.Signalling;

public sealed partial class SignalMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline =>
        {
            _ = pipeline.Use(new TestSignalMiddleware<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestSignalMiddleware<TestSignal>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline =>
        {
            _ = pipeline.Use(new TestSignalMiddleware<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestSignalMiddleware<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestSignalMiddleware<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestSignalMiddleware<TestSignal>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20, 40, 60 }));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline =>
        {
            _ = pipeline.Use(new TestSignalMiddlewareSub<TestSignal>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestSignalMiddlewareBase<TestSignal>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenUnusedMiddleware_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddSignalHandler<TestSignalHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<ISignalPipeline<TestSignal>>>(pipeline => { _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestSignalMiddleware<TestSignal>>(c => c.Parameter += 10)); });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ISignalPublishers>()
                              .For(TestSignal.T);

        await handler.Handle(new(), CancellationToken.None);
    }

    [Signal]
    private sealed partial record TestSignal;

    private sealed class TestSignalHandler : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline<T>(ISignalPipeline<T> pipeline)
            where T : class, ISignal<T>
        {
            if (pipeline is ISignalPipeline<TestSignal> p)
            {
                pipeline.ServiceProvider.GetService<Action<ISignalPipeline<TestSignal>>>()?.Invoke(p);
            }
        }
    }

    private sealed class TestSignalMiddleware<TSignal>(TestObservations observations) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public int Parameter { get; set; }

        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);
        }
    }

    private sealed class TestSignalMiddlewareSub<TSignal>(TestObservations observations) : TestSignalMiddlewareBase<TSignal>(observations)
        where TSignal : class, ISignal<TSignal>;

    private abstract class TestSignalMiddlewareBase<TSignal>(TestObservations observations) : ISignalMiddleware<TSignal>
        where TSignal : class, ISignal<TSignal>
    {
        public int Parameter { get; set; }

        public async Task Execute(SignalMiddlewareContext<TSignal> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await ctx.Next(ctx.Signal, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }
}
