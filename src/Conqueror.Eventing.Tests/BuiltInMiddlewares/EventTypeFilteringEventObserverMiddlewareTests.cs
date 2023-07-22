namespace Conqueror.Eventing.Tests.BuiltInMiddlewares;

public sealed class EventTypeFilteringEventObserverMiddlewareTests
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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithoutMiddleware_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<PolymorphicTestEventBase>(async (evt, p, _) =>
                    {
                        await Task.Yield();

                        p.GetRequiredService<TestObservations>().Events.Add(evt);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));
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
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt, evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWithMiddlewareWithDefaultConfiguration_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<PolymorphicTestEventBaseObserver>()
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.UseEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithMiddlewareWithDefaultConfiguration_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<PolymorphicTestEventBase>(async (evt, p, _) =>
                                                                                 {
                                                                                     await Task.Yield();

                                                                                     p.GetRequiredService<TestObservations>().Events.Add(evt);
                                                                                 },
                                                                                 pipeline => pipeline.UseEventTypeFiltering())
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWhichObservesMultipleSubTypesWithMiddlewareWithDefaultConfiguration_ObserverIsCalledForEverySubTypeHandler()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiPolymorphicTestEventBaseObserver>()
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.UseEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt, evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWithDisallowedPolymorphism_ObserverIsNotCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<PolymorphicTestEventBaseObserver>()
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.UseEventTypeFiltering().DisallowInvocationWithSubTypes())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.Empty);

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.Empty);
    }

    [Test]
    public async Task GivenEventObserverDelegateWithDisallowedPolymorphism_ObserverIsNotCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<PolymorphicTestEventBase>(async (evt, p, _) =>
                                                                                 {
                                                                                     await Task.Yield();

                                                                                     p.GetRequiredService<TestObservations>().Events.Add(evt);
                                                                                 },
                                                                                 pipeline => pipeline.UseEventTypeFiltering().DisallowInvocationWithSubTypes())
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

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
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.UseEventTypeFiltering().DisallowInvocationWithSubTypes())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.HandleEvent(evt);

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
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.UseEventTypeFiltering().DisallowInvocationWithSubTypes().WithoutEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverDelegateWithAddedAndThenRemovedMiddleware_ObserverIsCalledForSubTypesOfEventType()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserverDelegate<PolymorphicTestEventBase>(async (evt, p, _) =>
                                                                                 {
                                                                                     await Task.Yield();

                                                                                     p.GetRequiredService<TestObservations>().Events.Add(evt);
                                                                                 },
                                                                                 pipeline => pipeline.UseEventTypeFiltering().DisallowInvocationWithSubTypes().WithoutEventTypeFiltering())
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenEventObserverWhichObservesMultipleSubTypesWithAddedAndThenRemovedMiddleware_ObserverIsCalledForEverySubTypeHandler()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiPolymorphicTestEventBaseObserver>()
                    .AddConquerorEventingEventTypeFilteringMiddleware()
                    .AddSingleton<Action<IEventObserverPipelineBuilder>>(pipeline => pipeline.UseEventTypeFiltering().DisallowInvocationWithSubTypes().WithoutEventTypeFiltering())
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<PolymorphicTestEvent2>>();
        var dispatcher = provider.GetRequiredService<IConquerorEventDispatcher>();

        var evt = new PolymorphicTestEvent2();

        await observer.HandleEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EquivalentTo(new[] { evt, evt, evt, evt }));
    }

    private sealed record PolymorphicTestEvent : PolymorphicTestEventBase;

    private abstract record PolymorphicTestEventBase;

    private sealed record PolymorphicTestEvent2 : PolymorphicTestEventBase2;

    private abstract record PolymorphicTestEventBase2 : PolymorphicTestEventBase3;

    private abstract record PolymorphicTestEventBase3;

    private sealed class PolymorphicTestEventBaseObserver : IEventObserver<PolymorphicTestEventBase>, IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public PolymorphicTestEventBaseObserver(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(PolymorphicTestEventBase evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Events.Add(evt);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventObserverPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class MultiPolymorphicTestEventBaseObserver : IEventObserver<PolymorphicTestEventBase2>,
                                                                 IEventObserver<PolymorphicTestEventBase3>,
                                                                 IConfigureEventObserverPipeline
    {
        private readonly TestObservations observations;

        public MultiPolymorphicTestEventBaseObserver(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task HandleEvent(PolymorphicTestEventBase2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Events.Add(evt);
        }

        public async Task HandleEvent(PolymorphicTestEventBase3 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Events.Add(evt);
        }

        public static void ConfigurePipeline(IEventObserverPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IEventObserverPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestObservations
    {
        public List<object> Events { get; } = new();
    }
}
