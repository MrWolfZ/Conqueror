using Conqueror.Eventing;

namespace Conqueror.Tests.Eventing;

public sealed class EventNotificationReceiverTests
{
    [Test]
    public async Task GivenHandlerWithReceiverConfiguration_WhenRunningReceiver_ReceiverGetsConfiguredCorrectly()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiver>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestEventNotificationTransportReceiver>();

        var configurations = await receiver.Run(CancellationToken.None);

        Assert.That(configurations, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport), new() { Parameter = 10, Parameter2 = 1 }),
            (typeof(TestEventNotification2WithTestTransport), new TestEventNotificationTransportReceiverConfiguration { Parameter = 20, Parameter2 = 2 }),
        }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfiguration_WhenRunningReceiverMultipleTimes_ReceiverGetsConfiguredCorrectlyOnce()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiver>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestEventNotificationTransportReceiver>();

        _ = await receiver.Run(CancellationToken.None);
        _ = await receiver.Run(CancellationToken.None);

        Assert.That(testObservations.ConfigureReceiverCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForMixedTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<MixedTestEventNotificationHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiver>()
                    .AddSingleton<TestEventNotificationTransport2Receiver>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestEventNotificationTransportReceiver>();
        var receiver2 = provider.GetRequiredService<TestEventNotificationTransport2Receiver>();

        var configurations1 = await receiver1.Run(CancellationToken.None);
        var configurations2 = await receiver2.Run(CancellationToken.None);

        Assert.That(configurations1, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport), new() { Parameter = 10, Parameter2 = 0 }),
            (typeof(TestEventNotification2WithTestTransport), new TestEventNotificationTransportReceiverConfiguration { Parameter = 20, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport2), new TestEventNotificationTransport2ReceiverConfiguration { Parameter = 100 }),
        }));
    }

    [Test]
    public async Task GivenHandlerWithReceiverConfigurationForEventNotificationTypeWithMultipleTransports_WhenRunningReceiver_ReceiverGetsCorrectConfigurationForTransport()
    {
        var services = new ServiceCollection();
        var testObservations = new TestObservations();

        _ = services.AddEventNotificationHandler<TestEventNotificationWithMultipleTransportsHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiver>()
                    .AddSingleton<TestEventNotificationTransport2Receiver>()
                    .AddSingleton(testObservations);

        var provider = services.BuildServiceProvider();

        var receiver1 = provider.GetRequiredService<TestEventNotificationTransportReceiver>();
        var receiver2 = provider.GetRequiredService<TestEventNotificationTransport2Receiver>();

        var configurations1 = await receiver1.Run(CancellationToken.None);
        var configurations2 = await receiver2.Run(CancellationToken.None);

        Assert.That(configurations1, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithMultipleTestTransports), new TestEventNotificationTransportReceiverConfiguration { Parameter = 10, Parameter2 = 0 }),
        }));

        Assert.That(configurations2, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithMultipleTestTransports), new TestEventNotificationTransport2ReceiverConfiguration { Parameter = 20 }),
        }));
    }

    [Test]
    public void GivenEventNotificationReceiverBuilder_WhenConfiguringTheSameEventNotificationTypesMultipleTimes_LastConfigurationWins()
    {
        var provider = new ServiceCollection().AddEventNotificationHandler<TestEventNotificationHandler>().BuildServiceProvider();

        var registry = provider.GetRequiredService<IEventNotificationTransportRegistry>();

        var receiver = new EventNotificationReceiverBuilder(provider, registry, [
            typeof(TestEventNotificationWithTestTransport),
            typeof(TestEventNotification2WithTestTransport),
        ]);

        _ = receiver.UseTestTransport(10).WithParameter2(1);
        _ = receiver.UseTestTransport(20);

        _ = receiver.For<TestEventNotification2WithTestTransport>().UseTestTransport(100).WithParameter2(2);
        _ = receiver.For<TestEventNotification2WithTestTransport>().UseTestTransport(200);

        var configurations = receiver.GetConfigurationsByNotificationType();

        var expectedConfigurations = new Dictionary<Type, IReadOnlyCollection<IEventNotificationReceiverConfiguration>>
        {
            {
                typeof(TestEventNotificationWithTestTransport), new List<IEventNotificationReceiverConfiguration>
                {
                    new TestEventNotificationTransportReceiverConfiguration { Parameter = 20, Parameter2 = 0 },
                }
            },
            {
                typeof(TestEventNotification2WithTestTransport), new List<IEventNotificationReceiverConfiguration>
                {
                    new TestEventNotificationTransportReceiverConfiguration { Parameter = 200, Parameter2 = 0 },
                }
            },
        };

        Assert.That(configurations, Is.EquivalentTo(expectedConfigurations));
    }

    private sealed class TestEventNotificationHandler : TestEventNotificationWithTestTransport.IHandler, TestEventNotification2WithTestTransport.IHandler
    {
        public Task Handle(TestEventNotificationWithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotification2WithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureReceiver(IEventNotificationReceiver receiver)
        {
            Assert.That(receiver.EventNotificationTypes, Is.EquivalentTo(new[] { typeof(TestEventNotificationWithTestTransport), typeof(TestEventNotification2WithTestTransport) }));

            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.UseTestTransport(10).WithParameter2(1);
            _ = receiver.For<TestEventNotification2WithTestTransport>().UseTestTransport(20).WithParameter2(2);
            return Task.CompletedTask;
        }
    }

    private sealed class MixedTestEventNotificationHandler
        : TestEventNotificationWithTestTransport.IHandler,
          TestEventNotification2WithTestTransport.IHandler,
          TestEventNotificationWithTestTransport2.IHandler
    {
        public Task Handle(TestEventNotificationWithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotification2WithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotificationWithTestTransport2 notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureReceiver(IEventNotificationReceiver receiver)
        {
            Assert.That(receiver.EventNotificationTypes, Is.EquivalentTo(new[]
            {
                typeof(TestEventNotificationWithTestTransport),
                typeof(TestEventNotification2WithTestTransport),
                typeof(TestEventNotificationWithTestTransport2),
            }));

            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            _ = receiver.UseTestTransport(10);
            _ = receiver.For<TestEventNotification2WithTestTransport>().UseTestTransport(20);
            receiver.For<TestEventNotificationWithTestTransport2>().UseTestTransport2(100);
            return Task.CompletedTask;
        }
    }

    private sealed class TestEventNotificationWithMultipleTransportsHandler : TestEventNotificationWithMultipleTestTransports.IHandler
    {
        public Task Handle(TestEventNotificationWithMultipleTestTransports notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureReceiver(IEventNotificationReceiver receiver)
        {
            Assert.That(receiver.EventNotificationTypes, Is.EquivalentTo(new[] { typeof(TestEventNotificationWithMultipleTestTransports) }));

            var testObservations = receiver.ServiceProvider.GetRequiredService<TestObservations>();
            testObservations.ConfigureReceiverCallCount += 1;

            var eventNotificationReceiver = receiver.For<TestEventNotificationWithMultipleTestTransports>();

            _ = eventNotificationReceiver.UseTestTransport(10);
            eventNotificationReceiver.UseTestTransport2(20);

            return Task.CompletedTask;
        }
    }

    private sealed class TestObservations
    {
        public int ConfigureReceiverCallCount { get; set; }
    }
}

[EventNotification]
public sealed partial record TestEventNotificationWithTestTransport : ITestTransportEventNotification<TestEventNotificationWithTestTransport>
{
    static ITestTransportTypesInjector ITestTransportEventNotification.TestTransportTypesInjector
        => TestTransportTypesInjector<TestEventNotificationWithTestTransport>.Default;
}

[EventNotification]
public sealed partial record TestEventNotification2WithTestTransport : ITestTransportEventNotification<TestEventNotification2WithTestTransport>
{
    static ITestTransportTypesInjector ITestTransportEventNotification.TestTransportTypesInjector
        => TestTransportTypesInjector<TestEventNotification2WithTestTransport>.Default;
}

[EventNotification]
public sealed partial record TestEventNotificationWithTestTransport2 : ITestTransport2EventNotification<TestEventNotificationWithTestTransport2>
{
    static ITestTransport2TypesInjector ITestTransport2EventNotification.TestTransport2TypesInjector
        => TestTransport2TypesInjector<TestEventNotificationWithTestTransport2>.Default;
}

[EventNotification]
public sealed partial record TestEventNotificationWithMultipleTestTransports : ITestTransportEventNotification<TestEventNotificationWithMultipleTestTransports>,
                                                                               ITestTransport2EventNotification<TestEventNotificationWithMultipleTestTransports>
{
    static ITestTransportTypesInjector ITestTransportEventNotification.TestTransportTypesInjector
        => TestTransportTypesInjector<TestEventNotificationWithMultipleTestTransports>.Default;

    static ITestTransport2TypesInjector ITestTransport2EventNotification.TestTransport2TypesInjector
        => TestTransport2TypesInjector<TestEventNotificationWithMultipleTestTransports>.Default;
}

file sealed class TestEventNotificationTransportReceiver(IEventNotificationTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type NotificationType, TestEventNotificationTransportReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = await registry.GetEventNotificationInvokersForReceiver<ITestTransportTypesInjector, TestEventNotificationTransportReceiverConfiguration>(cancellationToken);
        return invokers.SelectMany(i => i.HandledEventNotificationTypes).Select(t => (t.TypesInjector.EventNotificationType, t.ReceiverConfiguration)).ToList();
    }
}

file sealed class TestEventNotificationTransport2Receiver(IEventNotificationTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type NotificationType, TestEventNotificationTransport2ReceiverConfiguration Configuration)>> Run(CancellationToken cancellationToken)
    {
        var invokers = await registry.GetEventNotificationInvokersForReceiver<ITestTransport2TypesInjector, TestEventNotificationTransport2ReceiverConfiguration>(cancellationToken);
        return invokers.SelectMany(i => i.HandledEventNotificationTypes).Select(t => (t.TypesInjector.EventNotificationType, t.ReceiverConfiguration)).ToList();
    }
}

public interface ITestTransportEventNotification
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransportTypesInjector TestTransportTypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransport2EventNotification
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransport2TypesInjector TestTransport2TypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransportEventNotification<T> : IEventNotification<T>, ITestTransportEventNotification
    where T : class, ITestTransportEventNotification<T>
{
    [SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "testing")]
    static virtual int DefaultParameter { get; } = -1;
}

public interface ITestTransport2EventNotification<T> : IEventNotification<T>, ITestTransport2EventNotification
    where T : class, ITestTransport2EventNotification<T>;

public interface ITestTransportTypesInjector : IEventNotificationTypesInjector
{
    TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable);
}

public interface ITestTransport2TypesInjector : IEventNotificationTypesInjector
{
    TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable);
}

file sealed class TestTransportTypesInjector<TEventNotification> : ITestTransportTypesInjector
    where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
{
    public static readonly TestTransportTypesInjector<TEventNotification> Default = new();

    public Type EventNotificationType { get; } = typeof(TEventNotification);

    public TResult CreateForTestTransport<TResult>(ITestTransportTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TEventNotification>();
}

file sealed class TestTransport2TypesInjector<TEventNotification> : ITestTransport2TypesInjector
    where TEventNotification : class, ITestTransport2EventNotification<TEventNotification>
{
    public static readonly TestTransport2TypesInjector<TEventNotification> Default = new();

    public Type EventNotificationType { get; } = typeof(TEventNotification);

    public TResult CreateForTestTransport2<TResult>(ITestTransport2TypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TEventNotification>();
}

public interface ITestTransportTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TEventNotification>()
        where TEventNotification : class, ITestTransportEventNotification<TEventNotification>;
}

public interface ITestTransport2TypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TEventNotification>()
        where TEventNotification : class, ITestTransport2EventNotification<TEventNotification>;
}

file interface ITestTransportConfigurationBuilder
{
    internal ITestTransportConfigurationBuilder Init(int? parameter);

    ITestTransportConfigurationBuilder WithParameter2(int parameter2);
}

file sealed class TestTransportConfigurationBuilder(IEventNotificationReceiver receiver) : ITestTransportConfigurationBuilder
{
    public ITestTransportConfigurationBuilder Init(int? parameter) => receiver.ConfigureTypedBuilder(b => b.Init(parameter));

    public ITestTransportConfigurationBuilder WithParameter2(int parameter2) => receiver.ConfigureTypedBuilder(b => b.WithParameter2(parameter2));
}

file sealed class TestTransportConfigurationBuilder<T>(IEventNotificationReceiver<T> receiver) : ITestTransportConfigurationBuilder
    where T : class, ITestTransportEventNotification<T>
{
    public ITestTransportConfigurationBuilder Init(int? parameter)
    {
        receiver.SetConfiguration(new TestEventNotificationTransportReceiverConfiguration { Parameter = parameter ?? T.DefaultParameter });
        return this;
    }

    public ITestTransportConfigurationBuilder WithParameter2(int parameter2)
    {
        receiver.UpdateConfiguration<TestEventNotificationTransportReceiverConfiguration>(c => c.Parameter2 = parameter2);
        return this;
    }
}

file static class TestEventNotificationExtensions
{
    internal static ITestTransportConfigurationBuilder ConfigureTypedBuilder(this IEventNotificationReceiver receiver,
                                                                             Action<ITestTransportConfigurationBuilder> configure)
    {
        receiver.UseTransport<ITestTransportTypesInjector>(i => configure(i.CreateForTestTransport(new TestEventTransportTypesInjectable(receiver))));
        return new TestTransportConfigurationBuilder(receiver);
    }

    public static ITestTransportConfigurationBuilder UseTestTransport(this IEventNotificationReceiver receiver, int? parameter)
        => receiver.ConfigureTypedBuilder(b => b.Init(parameter));

    public static ITestTransportConfigurationBuilder UseTestTransport<T>(this IEventNotificationReceiver<T> receiver, int? parameter)
        where T : class, ITestTransportEventNotification<T>
        => new TestTransportConfigurationBuilder<T>(receiver).Init(parameter);

    public static void UseTestTransport2<T>(this IEventNotificationReceiver<T> receiver, int parameter)
        where T : class, ITestTransport2EventNotification<T>
    {
        receiver.SetConfiguration(new TestEventNotificationTransport2ReceiverConfiguration { Parameter = parameter });
    }
}

file sealed class TestEventTransportTypesInjectable(IEventNotificationReceiver receiver) : ITestTransportTypesInjectable<ITestTransportConfigurationBuilder>
{
    public ITestTransportConfigurationBuilder WithInjectedTypes<TEventNotification>()
        where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
        => new TestTransportConfigurationBuilder<TEventNotification>(receiver.For<TEventNotification>());
}

file sealed record TestEventNotificationTransportReceiverConfiguration : IEventNotificationReceiverConfiguration
{
    public required int Parameter { get; set; }

    public int Parameter2 { get; set; }
}

file sealed record TestEventNotificationTransport2ReceiverConfiguration : IEventNotificationReceiverConfiguration
{
    public required int Parameter { get; set; }
}
