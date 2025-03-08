namespace Conqueror.Eventing.Tests;

public sealed class EventMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
        {
            _ = pipeline.Use(new TestEventMiddleware<TestEvent>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestEventMiddleware<TestEvent>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
        {
            _ = pipeline.Use(new TestEventMiddleware<TestEvent>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestEventMiddleware<TestEvent>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestEventMiddleware<TestEvent>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestEventMiddleware<TestEvent>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20, 40, 60 }));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
        {
            _ = pipeline.Use(new TestEventMiddlewareSub<TestEvent>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestEventMiddlewareBase<TestEvent>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EquivalentTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenUnusedMiddleware_WhenConfiguringMiddleware_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventPipeline<TestEvent>>>(pipeline =>
        {
            _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestEventMiddleware<TestEvent>>(c => c.Parameter += 10));
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await handler.Handle(new(), CancellationToken.None);
    }

    private sealed record TestEvent;

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline(IEventPipeline<TestEvent> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<TestEvent>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestEventMiddleware<TEvent>(TestObservations observations) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public int Parameter { get; set; }

        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestEventMiddlewareSub<TEvent>(TestObservations observations) : TestEventMiddlewareBase<TEvent>(observations)
        where TEvent : class;

    private abstract class TestEventMiddlewareBase<TEvent>(TestObservations observations) : IEventMiddleware<TEvent>
        where TEvent : class
    {
        public int Parameter { get; set; }

        public async Task Execute(EventMiddlewareContext<TEvent> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await ctx.Next(ctx.Event, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }
}
