namespace Conqueror.Eventing.Tests.Observing;

public abstract class EventObserverFunctionalityTests
{
    protected abstract IServiceCollection RegisterObserver(IServiceCollection services);

    protected abstract IServiceCollection RegisterObserver2(IServiceCollection services);

    protected virtual IEventObserver<TestEvent> ResolveObserver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IEventObserver<TestEvent>>();
    }

    protected virtual TestEvent CreateEvent() => new(10);

    protected virtual TestEvent CreateSubEvent() => new(10);

    [Test]
    public async Task GivenObserverForSingleEventType_WhenCalledWithEvent_ObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = RegisterObserver(services).AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenObserverForSingleEventType_WhenCalledWithEventOfSubType_ObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = RegisterObserver(services).AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateSubEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));
    }

    [Test]
    public async Task GivenMultipleObserversForSingleEventType_WhenCalledWithEvent_AllObserversReceiveEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = RegisterObserver2(RegisterObserver(services)).AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.EqualTo(new[] { evt, evt, evt, evt }));
    }

    [Test]
    public async Task GivenObserver_WhenCalledWithCancellationToken_ObserverReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = RegisterObserver(services).AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        using var cts = new CancellationTokenSource();

        await observer.Handle(evt, cts.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { cts.Token }));

        await dispatcher.DispatchEvent(evt, cts.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { cts.Token, cts.Token }));
    }

    [Test]
    public async Task GivenObserver_WhenCalledWithoutCancellationToken_ObserverReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = RegisterObserver(services).AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer.Handle(evt);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { CancellationToken.None }));

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { CancellationToken.None, CancellationToken.None }));
    }

    [Test]
    public void GivenObserver_WhenCallingThrowsException_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = RegisterObserver(services).AddSingleton(new TestObservations())
                                      .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var observer = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        Assert.That(() => observer.Handle(CreateEvent()), Throws.Exception.SameAs(exception));
        Assert.That(() => dispatcher.DispatchEvent(CreateEvent()), Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenObserver_WhenResolvingObserver_ObserverIsResolvedFromResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = RegisterObserver(services).AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = ResolveObserver(scope1.ServiceProvider);
        var observer2 = ResolveObserver(scope2.ServiceProvider);

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer1.Handle(evt);
        await observer1.Handle(evt);
        await observer2.Handle(evt);

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1])
                                                        .And.Not.SameAs(observations.ServiceProviders[2]));

        await dispatcher1.DispatchEvent(evt);
        await dispatcher1.DispatchEvent(evt);
        await dispatcher2.DispatchEvent(evt);

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(6));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1])
                                                        .And.SameAs(observations.ServiceProviders[3])
                                                        .And.SameAs(observations.ServiceProviders[4])
                                                        .And.Not.SameAs(observations.ServiceProviders[2])
                                                        .And.Not.SameAs(observations.ServiceProviders[5]));

        Assert.That(observations.ServiceProviders[2], Is.SameAs(observations.ServiceProviders[5]));
    }

    public record TestEvent(int Payload);

    public record TestEventSub(int Payload) : TestEvent(Payload);

    protected sealed class TestObservations
    {
        public List<object> Events { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];
    }
}

[TestFixture]
public sealed class EventObserverFunctionalityDefaultTests : EventObserverFunctionalityTests
{
    [Test]
    public async Task GivenEventTypeWithoutRegisteredObserver_WhenCallingObserver_LeadsToNoop()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer.Handle(evt);

        Assert.That(observations.Events, Is.Empty);

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Events, Is.Empty);
    }

    [Test]
    public async Task GivenObserverForMultipleEventTypes_WhenCalledWithEventOfEitherType_ObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiTestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt1 = new TestEvent(10);
        var evt2 = new TestEvent2(20);

        await observer1.Handle(evt1);
        await observer2.Handle(evt2);

        Assert.That(observations.Events, Is.EqualTo(new object[] { evt1, evt2 }));

        await dispatcher.DispatchEvent(evt1);
        await dispatcher.DispatchEvent(evt2);

        Assert.That(observations.Events, Is.EqualTo(new object[] { evt1, evt2, evt1, evt2 }));
    }

    [Test]
    public async Task GivenObserverForMultipleEventTypes_WhenCalledWithEventOfSubTypeOfEitherType_ObserverReceivesEvent()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<MultiTestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer1 = provider.GetRequiredService<IEventObserver<TestEvent>>();
        var observer2 = provider.GetRequiredService<IEventObserver<TestEvent2>>();
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt1 = new TestEventSub(10);
        var evt2 = new TestEvent2Sub(20);

        await observer1.Handle(evt1);
        await observer2.Handle(evt2);

        Assert.That(observations.Events, Is.EqualTo(new object[] { evt1, evt2 }));

        await dispatcher.DispatchEvent(evt1);
        await dispatcher.DispatchEvent(evt2);

        Assert.That(observations.Events, Is.EqualTo(new object[] { evt1, evt2, evt1, evt2 }));
    }

    [Test]
    public async Task GivenDisposableObserver_WhenServiceProviderIsDisposed_ThenObserverIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services.AddConquerorEventObserver<DisposableEventObserver>()
                    .AddSingleton(observation)
                    .AddSingleton(new TestObservations());

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventObserver<TestEvent>>();

        await handler.Handle(new(10));

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    protected override IServiceCollection RegisterObserver(IServiceCollection services)
    {
        return services.AddConquerorEventObserver<TestEventObserver>();
    }

    protected override IServiceCollection RegisterObserver2(IServiceCollection services)
    {
        return services.AddConquerorEventObserver<TestEventObserver2>();
    }

    public record TestEvent2(int Payload);

    public sealed record TestEvent2Sub(int Payload) : TestEvent2(Payload);

    private sealed class TestEventObserver(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestEventObserver2(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class MultiTestEventObserver(TestObservations observations, IServiceProvider serviceProvider) : IEventObserver<TestEvent>,
                                                                                                                   IEventObserver<TestEvent2>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public async Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class DisposableEventObserver(DisposalObservation observation) : IEventObserver<TestEvent>, IDisposable
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
        }

        public void Dispose() => observation.WasDisposed = true;
    }

    private sealed class DisposalObservation
    {
        public bool WasDisposed { get; set; }
    }
}

[TestFixture]
public sealed class EventObserverFunctionalityDelegateTests : EventObserverFunctionalityTests
{
    protected override IServiceCollection RegisterObserver(IServiceCollection services)
    {
        return services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
        {
            await Task.Yield();

            var obs = p.GetRequiredService<TestObservations>();
            obs.Events.Add(evt);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);

            if (p.GetService<Exception>() is { } e)
            {
                throw e;
            }
        });
    }

    protected override IServiceCollection RegisterObserver2(IServiceCollection services)
    {
        return services.AddConquerorEventObserverDelegate<TestEvent>(async (evt, p, cancellationToken) =>
        {
            await Task.Yield();

            var obs = p.GetRequiredService<TestObservations>();
            obs.Events.Add(evt);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);

            if (p.GetService<Exception>() is { } e)
            {
                throw e;
            }
        });
    }
}

[TestFixture]
public sealed class EventObserverFunctionalityGenericTests : EventObserverFunctionalityTests
{
    protected override IServiceCollection RegisterObserver(IServiceCollection services)
    {
        return services.AddConquerorEventObserver<GenericTestEventObserver<string>>();
    }

    protected override IServiceCollection RegisterObserver2(IServiceCollection services)
    {
        return services.AddConquerorEventObserver<GenericTestEventObserver2<string>>();
    }

    protected override IEventObserver<TestEvent> ResolveObserver(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<IEventObserver<GenericTestEvent<string>>>();
        return new AdapterObserver<string>(handler);
    }

    protected override TestEvent CreateEvent() => new GenericTestEvent<string>("test");

    protected override TestEvent CreateSubEvent() => new GenericTestEventSub<string>("test");

    private record GenericTestEvent<T>(T GenericPayload) : TestEvent(10);

    private sealed record GenericTestEventSub<T>(T GenericPayload) : GenericTestEvent<T>(GenericPayload);

    private sealed class GenericTestEventObserver<T>(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : IEventObserver<GenericTestEvent<T>>
    {
        public async Task Handle(GenericTestEvent<T> evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class GenericTestEventObserver2<T>(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : IEventObserver<GenericTestEvent<T>>
    {
        public async Task Handle(GenericTestEvent<T> evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class AdapterObserver<T>(IEventObserver<GenericTestEvent<T>> wrapped) : IEventObserver<TestEvent>
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await wrapped.Handle((GenericTestEvent<T>)evt, cancellationToken);
        }
    }
}

[TestFixture]
public sealed class EventObserverFunctionalityCustomInterfaceTests : EventObserverFunctionalityTests
{
    [Test]
    public void GivenObserverWithCustomInterface_ObserverCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventObserver<TestEventObserver>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IEventObserver<TestEvent>>());
    }

    protected override IServiceCollection RegisterObserver(IServiceCollection services)
    {
        return services.AddConquerorEventObserver<TestEventObserver>();
    }

    protected override IServiceCollection RegisterObserver2(IServiceCollection services)
    {
        return services.AddConquerorEventObserver<TestEventObserver2>();
    }

    protected override IEventObserver<TestEvent> ResolveObserver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ITestEventObserver>();
    }

    public interface ITestEventObserver : IEventObserver<TestEvent>;

    private sealed class TestEventObserver(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : ITestEventObserver
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed class TestEventObserver2(TestObservations observations, IServiceProvider serviceProvider, Exception? exceptionToThrow = null) : ITestEventObserver
    {
        public async Task Handle(TestEvent evt, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.Events.Add(evt);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }
}

[TestFixture]
public sealed class EventObserverFunctionalityTransportTests : EventObserverFunctionalityTests
{
    protected override IServiceCollection RegisterObserver(IServiceCollection services)
    {
        services.TryAddSingleton(new TransportInvocationCount(1));
        return services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>();
    }

    protected override IServiceCollection RegisterObserver2(IServiceCollection services)
    {
        _ = services.Replace(ServiceDescriptor.Singleton(new TransportInvocationCount(2)));
        return services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>();
    }

    protected override TestEvent CreateEvent() => new TestEventWithCustomTransport("test");

    protected override TestEvent CreateSubEvent() => new TestEventWithCustomTransportSub("test");

    [Test]
    public async Task GivenEventTypeWithCustomTransportConfiguration_WhenPublishingEvent_PublisherReceivesConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TransportObservation();

        _ = RegisterObserver(services).AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                                      .AddSingleton(new TestObservations())
                                      .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var observer = ResolveObserver(provider);
        var dispatcher = provider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer.Handle(evt);

        Assert.That(observations.Attribute?.Parameter, Is.EqualTo(10));

        observations.Attribute = null;

        await dispatcher.DispatchEvent(evt);

        Assert.That(observations.Attribute?.Parameter, Is.EqualTo(10));
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransportConfiguration_WhenPublishingEvent_PublisherIsResolvedFromResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TransportObservation();

        _ = RegisterObserver(services).AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                                      .AddSingleton(new TestObservations())
                                      .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = ResolveObserver(scope1.ServiceProvider);
        var observer2 = ResolveObserver(scope2.ServiceProvider);

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer1.Handle(evt);
        await observer1.Handle(evt);
        await observer2.Handle(evt);

        Assert.That(observations.ServiceProvidersFromInstance, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromInstance[0], Is.SameAs(observations.ServiceProvidersFromInstance[1])
                                                                    .And.Not.SameAs(observations.ServiceProvidersFromInstance[2]));

        await dispatcher1.DispatchEvent(evt);
        await dispatcher1.DispatchEvent(evt);
        await dispatcher2.DispatchEvent(evt);

        Assert.That(observations.ServiceProvidersFromInstance, Has.Count.EqualTo(6));
        Assert.That(observations.ServiceProvidersFromInstance[0], Is.SameAs(observations.ServiceProvidersFromInstance[1])
                                                                    .And.SameAs(observations.ServiceProvidersFromInstance[3])
                                                                    .And.SameAs(observations.ServiceProvidersFromInstance[4])
                                                                    .And.Not.SameAs(observations.ServiceProvidersFromInstance[2])
                                                                    .And.Not.SameAs(observations.ServiceProvidersFromInstance[5]));

        Assert.That(observations.ServiceProvidersFromInstance[2], Is.SameAs(observations.ServiceProvidersFromInstance[5]));
    }

    [Test]
    public async Task GivenEventTypeWithCustomTransportConfiguration_WhenPublishingEvent_PublisherIsCalledWithServiceProviderFromResolutionScope()
    {
        var services = new ServiceCollection();
        var observations = new TransportObservation();

        _ = RegisterObserver(services).AddConquerorEventTransportPublisher<TestEventTransportPublisher>()
                                      .AddSingleton(new TestObservations())
                                      .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var observer1 = ResolveObserver(scope1.ServiceProvider);
        var observer2 = ResolveObserver(scope2.ServiceProvider);

        var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IEventDispatcher>();
        var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IEventDispatcher>();

        var evt = CreateEvent();

        await observer1.Handle(evt);
        await observer1.Handle(evt);
        await observer2.Handle(evt);

        Assert.That(observations.ServiceProvidersFromPublish, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromPublish[0], Is.SameAs(observations.ServiceProvidersFromPublish[1])
                                                                   .And.Not.SameAs(observations.ServiceProvidersFromPublish[2]));

        await dispatcher1.DispatchEvent(evt);
        await dispatcher1.DispatchEvent(evt);
        await dispatcher2.DispatchEvent(evt);

        Assert.That(observations.ServiceProvidersFromPublish, Has.Count.EqualTo(6));
        Assert.That(observations.ServiceProvidersFromPublish[0], Is.SameAs(observations.ServiceProvidersFromPublish[1])
                                                                   .And.SameAs(observations.ServiceProvidersFromPublish[3])
                                                                   .And.SameAs(observations.ServiceProvidersFromPublish[4])
                                                                   .And.Not.SameAs(observations.ServiceProvidersFromPublish[2])
                                                                   .And.Not.SameAs(observations.ServiceProvidersFromPublish[5]));

        Assert.That(observations.ServiceProvidersFromPublish[2], Is.SameAs(observations.ServiceProvidersFromPublish[5]));
    }

    [TestEventTransport(Parameter = 10)]
    private sealed record TestEventWithCustomTransport(string ExtraPayload) : TestEvent(10);

    [TestEventTransport(Parameter = 20)]
    private sealed record TestEventWithCustomTransportSub(string ExtraPayload) : TestEvent(10);

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute() : EventTransportAttribute(nameof(TestEventTransportAttribute))
    {
        public int Parameter { get; set; }
    }

    private sealed class TestEventTransportPublisher(
        TestObservations observations,
        IServiceProvider serviceProviderFromInstance,
        TransportInvocationCount invocationCount,
        TransportObservation? transportObservation = null,
        Exception? exceptionToThrow = null) : IEventTransportPublisher<TestEventTransportAttribute>
    {
        public async Task PublishEvent(object evt, TestEventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Yield();

            Assert.That(attribute.Parameter, Is.EqualTo(evt is TestEventWithCustomTransport ? 10 : 20));

            // the base test class has a test for multiple observers, which we simulate here
            for (var i = 0; i < invocationCount.InvocationCount; i++)
            {
                observations.Events.Add(evt);
                observations.CancellationTokens.Add(cancellationToken);
                observations.ServiceProviders.Add(serviceProvider);
            }

            if (transportObservation != null)
            {
                transportObservation.Attribute = attribute;
                transportObservation.ServiceProvidersFromPublish.Add(serviceProvider);
                transportObservation.ServiceProvidersFromInstance.Add(serviceProviderFromInstance);
            }

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }
        }
    }

    private sealed record TransportInvocationCount(int InvocationCount);

    private sealed class TransportObservation
    {
        public TestEventTransportAttribute? Attribute { get; set; }

        public List<IServiceProvider> ServiceProvidersFromInstance { get; } = [];

        public List<IServiceProvider> ServiceProvidersFromPublish { get; } = [];
    }
}
