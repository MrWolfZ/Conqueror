namespace Conqueror.Eventing.Tests.BuiltInMiddlewares;

public sealed class EventTypeFilteringEventMiddlewareTests
{
    [Test]
    public async Task GivenEventObserverWithoutMiddleware_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<PolymorphicTestEventBaseObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWhichObservesMultipleSubTypesWithoutMiddleware_ObserverIsCalledForEverySubTypeHandler()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiPolymorphicTestEventBaseObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt, evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWithMiddlewareWithDefaultConfiguration_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<PolymorphicTestEventBaseObserver>()
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase>>>(pipeline => pipeline.UseEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWhichObservesMultipleSubTypesWithMiddlewareWithDefaultConfiguration_ObserverIsCalledForEverySubTypeHandler()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiPolymorphicTestEventBaseObserver>()
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase2>>>(pipeline => pipeline.UseEventTypeFiltering())
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase3>>>(pipeline => pipeline.UseEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt, evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWithDisallowedPolymorphism_ObserverIsNotCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<PolymorphicTestEventBaseObserver>()
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase>>>(pipeline => pipeline.UseEventTypeFiltering()
                                                                                                        .DisallowInvocationWithSubTypes())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.Empty);

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.Empty);
    }

    [Test]
    public async Task GivenEventObserverWhichObservesMultipleSubTypesWithDisallowedPolymorphism_ObserverIsNotCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiPolymorphicTestEventBaseObserver>()
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase2>>>(pipeline => pipeline.UseEventTypeFiltering()
                                                                                                         .DisallowInvocationWithSubTypes())
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase3>>>(pipeline => pipeline.UseEventTypeFiltering()
                                                                                                         .DisallowInvocationWithSubTypes())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.Empty);

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.Empty);
    }

    [Test]
    public async Task GivenEventObserverWithAddedAndThenRemovedMiddleware_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<PolymorphicTestEventBaseObserver>()
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase>>>(pipeline => pipeline.UseEventTypeFiltering()
                                                                                                        .DisallowInvocationWithSubTypes()
                                                                                                        .WithoutEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWhichObservesMultipleSubTypesWithAddedAndThenRemovedMiddleware_ObserverIsCalledForEverySubTypeHandler()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiPolymorphicTestEventBaseObserver>()
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase2>>>(pipeline => pipeline.UseEventTypeFiltering()
                                                                                                         .DisallowInvocationWithSubTypes()
                                                                                                         .WithoutEventTypeFiltering())
                    .AddSingleton<Action<IEventPipeline<PolymorphicTestEventBase3>>>(pipeline => pipeline.UseEventTypeFiltering()
                                                                                                         .DisallowInvocationWithSubTypes()
                                                                                                         .WithoutEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt, evt, evt }));
    }

    private sealed record PolymorphicTestEvent : PolymorphicTestEventBase;

    private abstract record PolymorphicTestEventBase;

    private sealed record PolymorphicTestEvent2 : PolymorphicTestEventBase2;

    private abstract record PolymorphicTestEventBase2 : PolymorphicTestEventBase3;

    private abstract record PolymorphicTestEventBase3;

    private sealed class PolymorphicTestEventBaseObserver(TestObservations observations) : IEventObserver<PolymorphicTestEventBase>
    {
        public async Task Handle(PolymorphicTestEventBase evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Events.Add(evt);
        }

        public static void ConfigurePipeline(IEventPipeline<PolymorphicTestEventBase> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<PolymorphicTestEventBase>>>()?.Invoke(pipeline);
        }
    }

    private sealed class MultiPolymorphicTestEventBaseObserver(TestObservations observations) : IEventObserver<PolymorphicTestEventBase2>,
                                                                                                IEventObserver<PolymorphicTestEventBase3>
    {
        public async Task Handle(PolymorphicTestEventBase2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Events.Add(evt);
        }

        public async Task Handle(PolymorphicTestEventBase3 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Events.Add(evt);
        }

        public static void ConfigurePipeline(IEventPipeline<PolymorphicTestEventBase2> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<PolymorphicTestEventBase2>>>()?.Invoke(pipeline);
        }

        public static void ConfigurePipeline(IEventPipeline<PolymorphicTestEventBase3> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventPipeline<PolymorphicTestEventBase3>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestObservations
    {
        public List<object> Events { get; } = [];
    }
}
