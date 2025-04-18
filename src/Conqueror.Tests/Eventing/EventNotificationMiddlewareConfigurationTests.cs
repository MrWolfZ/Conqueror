namespace Conqueror.Tests.Eventing;

public sealed partial class EventNotificationMiddlewareConfigurationTests
{
    [Test]
    public async Task GivenMiddlewareWithParameter_WhenPipelineConfigurationUpdatesParameter_TheMiddlewareExecutesWithUpdatedParameter()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline =>
        {
            _ = pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestEventNotificationMiddleware<TestEventNotification>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenMultipleMiddlewareOfSameType_WhenPipelineConfigurationRuns_AllMiddlewaresAreUpdated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline =>
        {
            _ = pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });
            _ = pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 30 });
            _ = pipeline.Use(new TestEventNotificationMiddleware<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 50 });

            _ = pipeline.Configure<TestEventNotificationMiddleware<TestEventNotification>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20, 40, 60 }));
    }

    [Test]
    public async Task GivenMiddlewareWithBaseClass_WhenPipelineConfiguresBaseClass_TheMiddlewareIsConfigured()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline =>
        {
            _ = pipeline.Use(new TestEventNotificationMiddlewareSub<TestEventNotification>(pipeline.ServiceProvider.GetRequiredService<TestObservations>()) { Parameter = 10 });

            _ = pipeline.Configure<TestEventNotificationMiddlewareBase<TestEventNotification>>(c => c.Parameter += 10);
        });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.Handle(new());

        Assert.That(observations.Parameters, Is.EqualTo(new[] { 20 }));
    }

    [Test]
    public async Task GivenUnusedMiddleware_ConfiguringMiddlewareThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IEventNotificationPipeline<TestEventNotification>>>(pipeline => { _ = Assert.Throws<InvalidOperationException>(() => pipeline.Configure<TestEventNotificationMiddleware<TestEventNotification>>(c => c.Parameter += 10)); });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.Handle(new(), CancellationToken.None);
    }

    [EventNotification]
    private sealed partial record TestEventNotification;

    private sealed class TestEventNotificationHandler : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public static void ConfigurePipeline<T>(IEventNotificationPipeline<T> pipeline)
            where T : class, IEventNotification<T>
        {
            if (pipeline is IEventNotificationPipeline<TestEventNotification> p)
            {
                pipeline.ServiceProvider.GetService<Action<IEventNotificationPipeline<TestEventNotification>>>()?.Invoke(p);
            }
        }
    }

    private sealed class TestEventNotificationMiddleware<TEventNotification>(TestObservations observations) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public int Parameter { get; set; }

        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
        }
    }

    private sealed class TestEventNotificationMiddlewareSub<TEventNotification>(TestObservations observations) : TestEventNotificationMiddlewareBase<TEventNotification>(observations)
        where TEventNotification : class, IEventNotification<TEventNotification>;

    private abstract class TestEventNotificationMiddlewareBase<TEventNotification>(TestObservations observations) : IEventNotificationMiddleware<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public int Parameter { get; set; }

        public async Task Execute(EventNotificationMiddlewareContext<TEventNotification> ctx)
        {
            await Task.Yield();
            observations.Parameters.Add(Parameter);

            await ctx.Next(ctx.EventNotification, ctx.CancellationToken);
        }
    }

    private sealed class TestObservations
    {
        public List<int> Parameters { get; } = [];
    }
}
