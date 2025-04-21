namespace Conqueror.Tests.Eventing;

public abstract partial class EventNotificationHandlerFunctionalityTests
{
    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected abstract IServiceCollection RegisterHandler2(IServiceCollection services);

    protected virtual TestEventNotification.IHandler ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);
    }

    protected TestEventNotification CreateEventNotification() => new(10);

    protected TestEventNotification CreateSubEvent() => new(10);

    [Test]
    public async Task GivenHandlerForSingleNotificationType_WhenCalledWithEvent_HandlerReceivesNotification()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateEventNotification();

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.EqualTo(new[] { notification }));
    }

    [Test]
    public async Task GivenHandlerForSingleNotificationType_WhenCalledWithEventOfSubType_HandlerReceivesNotification()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateSubEvent();

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.EqualTo(new[] { notification }));
    }

    [Test]
    public async Task GivenMultipleHandlersForSingleNotificationType_WhenCalledWithEvent_AllHandlersReceiveEvent()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler2(RegisterHandler(new ServiceCollection()))
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateEventNotification();

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.EqualTo(new[] { notification, notification }));
    }

    [Test]
    public async Task GivenHandler_WhenCalledWithCancellationToken_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateEventNotification();

        using var cts = new CancellationTokenSource();

        await handler.Handle(notification, cts.Token);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { cts.Token }));
    }

    [Test]
    public async Task GivenHandler_WhenCalledWithoutCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateEventNotification();

        await handler.Handle(notification);

        Assert.That(observations.CancellationTokens, Is.EqualTo(new[] { CancellationToken.None }));
    }

    [Test]
    public void GivenHandler_WhenCallingThrowsException_InvocationThrowsSameException()
    {
        var exception = new Exception();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(new TestObservations())
                       .AddSingleton(exception)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        Assert.That(() => handler.Handle(CreateEventNotification()), Throws.Exception.SameAs(exception));
    }

    [Test]
    public async Task GivenHandler_WhenResolvingHandler_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        var notification = CreateEventNotification();

        await handler1.Handle(notification);
        await handler1.Handle(notification);
        await handler2.Handle(notification);

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1])
                                                        .And.Not.SameAs(observations.ServiceProviders[2]));
    }

    [EventNotification]
    public partial record TestEventNotification(int Payload);

    public sealed class TestObservations
    {
        public List<object> EventNotifications { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];

        public List<IServiceProvider> ServiceProvidersFromTransportFactory { get; } = [];
    }
}

[TestFixture]
public sealed partial class EventNotificationHandlerFunctionalityDefaultTests : EventNotificationHandlerFunctionalityTests
{
    [Test]
    public async Task GivenNotificationTypeWithoutRegisteredHandler_WhenCallingHandler_LeadsToNoop()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorEventing().AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateEventNotification();

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.Empty);
    }

    [Test]
    public async Task GivenNotificationTypeWithHandlerWithDisabledInProcessTransport_WhenCallingHandler_LeadsToNoop()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandlerWithDisabledInProcessTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var notification = CreateEventNotification();

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.Empty);
    }

    [Test]
    public async Task GivenHandlerForMultipleNotificationTypes_WhenCalledWithEventOfEitherType_HandlerReceivesNotification()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<MultiTestEventNotificationHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEventNotification.T);
        var handler2 = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEvent2.T);

        var notification1 = new TestEventNotification(10);
        var notification2 = new TestEvent2(20);

        await handler1.Handle(notification1);
        await handler2.Handle(notification2);

        Assert.That(observations.EventNotifications, Is.EqualTo(new object[] { notification1, notification2 }));
    }

    [Test]
    public async Task GivenHandlerForMultipleNotificationTypes_WhenCalledWithEventOfSubTypeOfEitherType_HandlerReceivesNotification()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<MultiTestEventNotificationHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler1 = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEventNotification.T);
        var handler2 = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEvent2.T);

        var notification1 = new TestEventNotificationSub(10);
        var notification2 = new TestEvent2Sub(20);

        await handler1.Handle(notification1);
        await handler2.Handle(notification2);

        Assert.That(observations.EventNotifications, Is.EqualTo(new object[] { notification1, notification2 }));
    }

    [Test]
    public async Task GivenHandlerForMultipleNotificationTypesFromTheSameHierarchy_WhenCalledWithEventOfSubType_HandlerReceivesNotificationMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<MultiHierarchyTestEventNotificationHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEventNotification.T);

        var notification = new TestEventNotificationSub(10);

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.EqualTo(new object[] { notification, notification }));
    }

    [Test]
    public async Task GivenHandlerForMultipleNotificationTypesFromTheSameHierarchy_WhenCalledWithEventOfSubSubType_HandlerReceivesNotificationMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddEventNotificationHandler<MultiHierarchyTestEventNotificationHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEventNotification.T);

        var notification = new TestEventNotificationSubSub(10);

        await handler.Handle(notification);

        Assert.That(observations.EventNotifications, Is.EqualTo(new object[] { notification, notification }));
    }

    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services.AddEventNotificationHandler<DisposableEventNotificationHandler>()
                    .AddSingleton(observation)
                    .AddSingleton(new TestObservations());

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>()
                              .For(TestEventNotification.T);

        await handler.Handle(new(10));

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddEventNotificationHandler<TestEventNotificationHandler>();
    }

    protected override IServiceCollection RegisterHandler2(IServiceCollection services)
    {
        return services.AddEventNotificationHandler<TestEventNotificationHandler2>();
    }

    [EventNotification]
    public partial record TestEventNotificationSub(int Payload) : TestEventNotification(Payload);

    public sealed record TestEventNotificationSubSub(int Payload) : TestEventNotificationSub(Payload);

    [EventNotification]
    public partial record TestEvent2(int Payload);

    public sealed record TestEvent2Sub(int Payload) : TestEvent2(Payload);

    private sealed class TestEventNotificationHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exceptionToThrow = null)
        : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class TestEventNotificationHandler2(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exceptionToThrow = null)
        : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exceptionToThrow is not null)
            {
                throw exceptionToThrow;
            }

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class MultiTestEventNotificationHandler(
        TestObservations observations,
        IServiceProvider serviceProvider)
        : TestEventNotification.IHandler,
          TestEvent2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public async Task Handle(TestEvent2 notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class MultiHierarchyTestEventNotificationHandler(
        TestObservations observations,
        IServiceProvider serviceProvider)
        : TestEventNotification.IHandler,
          TestEventNotificationSub.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public async Task Handle(TestEventNotificationSub notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class TestEventNotificationHandlerWithDisabledInProcessTransport(TestObservations observations) : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
        }

        static void IGeneratedEventNotificationHandler.ConfigureInProcessReceiver<T>(IInProcessEventNotificationReceiver<T> receiver) => receiver.Disable();
    }

    private sealed class DisposableEventNotificationHandler(DisposalObservation observation) : TestEventNotification.IHandler, IDisposable
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
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
public sealed class EventNotificationHandlerFunctionalityDelegateTests : EventNotificationHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddEventNotificationHandlerDelegate(
            TestEventNotification.T,
            async (notification, p, cancellationToken) =>
            {
                await Task.Yield();

                if (p.GetService<Exception>() is { } e)
                {
                    throw e;
                }

                var obs = p.GetRequiredService<TestObservations>();
                obs.EventNotifications.Add(notification);
                obs.CancellationTokens.Add(cancellationToken);
                obs.ServiceProviders.Add(p);
            });
    }

    protected override IServiceCollection RegisterHandler2(IServiceCollection services)
    {
        return services.AddEventNotificationHandlerDelegate(
            TestEventNotification.T,
            async (notification, p, cancellationToken) =>
            {
                await Task.Yield();

                if (p.GetService<Exception>() is { } e)
                {
                    throw e;
                }

                var obs = p.GetRequiredService<TestObservations>();
                obs.EventNotifications.Add(notification);
                obs.CancellationTokens.Add(cancellationToken);
                obs.ServiceProviders.Add(p);
            });
    }
}

[TestFixture]
public sealed partial class EventNotificationHandlerFunctionalityAssemblyScanningTests : EventNotificationHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddEventNotificationHandlersFromExecutingAssembly();
    }

    protected override IServiceCollection RegisterHandler2(IServiceCollection services)
    {
        return services.AddEventNotificationHandlersFromExecutingAssembly()
                       .AddEventNotificationHandler<TestEventNotificationHandler>();
    }

    [EventNotification]
    public sealed partial record TestEventNotification2(int Payload);

    // ReSharper disable once UnusedType.Global (accessed via reflection)
    public sealed class TestEventNotificationForAssemblyScanningHandler(
        TestObservations observations,
        IServiceProvider serviceProvider,
        Exception? exception = null)
        : TestEventNotification.IHandler,
          TestEventNotification2.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }

        public Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestEventNotificationHandler(
        TestObservations observations)
        : TestEventNotification.IHandler
    {
        public async Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.EventNotifications.Add(notification);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }
}

public abstract class EventNotificationHandlerFunctionalityPublisherTests : EventNotificationHandlerFunctionalityTests
{
    [Test]
    public async Task GivenHandlerClient_WhenCallingClient_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        await using var provider = RegisterHandler(new ServiceCollection())
                                   .AddSingleton(observations)
                                   .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        await handler1.Handle(CreateEventNotification());
        await handler1.Handle(CreateEventNotification());
        await handler2.Handle(CreateEventNotification());

        Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.SameAs(observations.ServiceProvidersFromTransportFactory[1]));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2]));
    }

    protected abstract TestEventNotification.IHandler ConfigureWithPublisher(
        TestEventNotification.IHandler builder,
        Func<IEventNotificationPublisherBuilder<TestEventNotification>, IEventNotificationPublisher<TestEventNotification>?>? baseConfigure = null);

    protected sealed override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        var existingOptions = services.Select(d => d.ImplementationInstance).OfType<TestEventNotificationPublisherOptions>().FirstOrDefault();
        _ = services.Replace(ServiceDescriptor.Singleton(new TestEventNotificationPublisherOptions(existingOptions?.HandlerCount + 1 ?? 1)));
        services.TryAddSingleton(typeof(TestEventNotificationPublisher<>));
        return services.AddConqueror();
    }

    protected sealed override IServiceCollection RegisterHandler2(IServiceCollection services)
        => RegisterHandler(services);

    protected sealed override TestEventNotification.IHandler ResolveHandler(IServiceProvider serviceProvider)
    {
        return ConfigureWithPublisher(base.ResolveHandler(serviceProvider));
    }

    protected sealed class TestEventNotificationPublisher<TEventNotification>(TestEventNotificationPublisherOptions options, Exception? exception = null)
        : IEventNotificationPublisher<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public string TransportTypeName => "test";

        public async Task Publish(TEventNotification notification,
                                  IServiceProvider serviceProvider,
                                  ConquerorContext conquerorContext,
                                  CancellationToken cancellationToken)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            var observations = serviceProvider.GetRequiredService<TestObservations>();

            for (var i = 0; i < options.HandlerCount; i += 1)
            {
                observations.EventNotifications.Add(notification);
                observations.CancellationTokens.Add(cancellationToken);
                observations.ServiceProviders.Add(serviceProvider);
            }
        }
    }

    protected sealed record TestEventNotificationPublisherOptions(int HandlerCount);
}

[TestFixture]
public sealed class EventNotificationHandlerFunctionalityPublisherWithSyncTransportFactoryTests : EventNotificationHandlerFunctionalityPublisherTests
{
    protected override TestEventNotification.IHandler ConfigureWithPublisher(
        TestEventNotification.IHandler builder,
        Func<IEventNotificationPublisherBuilder<TestEventNotification>, IEventNotificationPublisher<TestEventNotification>?>? baseConfigure = null)
    {
        return builder.WithPublisher(b =>
        {
            b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestEventNotificationPublisher<TestEventNotification>>();
        });
    }
}

[TestFixture]
public sealed class EventNotificationHandlerFunctionalityPublisherWithAsyncTransportFactoryTests : EventNotificationHandlerFunctionalityPublisherTests
{
    protected override TestEventNotification.IHandler ConfigureWithPublisher(
        TestEventNotification.IHandler builder,
        Func<IEventNotificationPublisherBuilder<TestEventNotification>, IEventNotificationPublisher<TestEventNotification>?>? baseConfigure = null)
    {
        return builder.WithPublisher(async b =>
        {
            await Task.Delay(1);
            b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
            return baseConfigure?.Invoke(b) ?? b.ServiceProvider.GetRequiredService<TestEventNotificationPublisher<TestEventNotification>>();
        });
    }
}
