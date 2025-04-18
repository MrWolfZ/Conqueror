namespace Conqueror.Tests.Eventing;

public sealed class EventNotificationReceiverTests
{
    [Test]
    public async Task GivenHandlerWithReceiverConfiguration_WhenRunningReceiver_ReceiverGetsConfiguredCorrectly()
    {
        var services = new ServiceCollection();

        _ = services.AddEventNotificationHandler<TestEventNotificationHandler>()
                    .AddSingleton<TestEventNotificationTransportReceiver>();

        var provider = services.BuildServiceProvider();

        var receiver = provider.GetRequiredService<TestEventNotificationTransportReceiver>();

        var configurations = await receiver.Run();

        Assert.That(configurations, Is.EquivalentTo(new[]
        {
            (typeof(TestEventNotificationWithTestTransport), new() { Parameter = 10, Parameter2 = 1 }),
            (typeof(TestEventNotification2WithTestTransport), new TestEventNotificationTransportReceiverConfiguration { Parameter = 20, Parameter2 = 2 }),
        }));
    }

    private sealed class TestEventNotificationHandler : TestEventNotificationWithTestTransport.IHandler, TestEventNotification2WithTestTransport.IHandler
    {
        public Task Handle(TestEventNotificationWithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotification2WithTestTransport notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public static Task ConfigureReceiver(IEventNotificationReceiver receiver)
        {
            Assert.That(receiver.EventNotificationTypes, Is.EquivalentTo(new[] { typeof(TestEventNotificationWithTestTransport), typeof(TestEventNotification2WithTestTransport) }));

            Assert.That(receiver.ServiceProvider.GetRequiredService<IEventNotificationPublishers>(), Is.Not.Null);

            _ = receiver.For<TestEventNotificationWithTestTransport>().UseTestTransport(10).WithParameter2(1);
            _ = receiver.UseTestTransport(20).WithParameter2(2);
            return Task.CompletedTask;
        }
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

file sealed class TestEventNotificationTransportReceiver(IEventNotificationTransportRegistry registry)
{
    public async Task<IReadOnlyCollection<(Type NotificationType, TestEventNotificationTransportReceiverConfiguration Configuration)>> Run()
    {
        var result = await registry.GetEventNotificationTypesForReceiver<ITestTransportTypesInjector, TestEventNotificationTransportReceiverConfiguration>();
        return result.Select(t => (t.EventNotificationType, t.ReceiverConfiguration)).ToList();
    }
}

public interface ITestTransportEventNotification
{
    // must be virtual instead of abstract so that it can be used as a type parameter / constraint
    // ReSharper disable once UnusedMember.Global (used by reflection)
    static virtual ITestTransportTypesInjector TestTransportTypesInjector
        => throw new NotSupportedException("this method should always be implemented by the generic interface");
}

public interface ITestTransportEventNotification<T> : IEventNotification<T>, ITestTransportEventNotification
    where T : class, ITestTransportEventNotification<T>;

public interface ITestTransportTypesInjector : IEventNotificationTypesInjector
{
    TResult CreateWithEventNotificationTypes<TResult>(ITestTransportTypesInjectable<TResult> injectable);
}

file sealed class TestTransportTypesInjector<TEventNotification> : ITestTransportTypesInjector
    where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
{
    public static readonly TestTransportTypesInjector<TEventNotification> Default = new();

    public TResult CreateWithEventNotificationTypes<TResult>(ITestTransportTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TEventNotification>();
}

public interface ITestTransportTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TEventNotification>()
        where TEventNotification : class, ITestTransportEventNotification<TEventNotification>;
}

file interface ITestTransportConfigurationBuilder
{
    ITestTransportConfigurationBuilder WithParameter2(int parameter2);
}

file sealed class TestTransportConfigurationBuilder<T>(IEventNotificationReceiver<T> receiver) : ITestTransportConfigurationBuilder
    where T : class, IEventNotification<T>
{
    public ITestTransportConfigurationBuilder WithParameter2(int parameter2)
    {
        receiver.UpdateConfiguration<TestEventNotificationTransportReceiverConfiguration>(c => c.Parameter2 = parameter2);
        return this;
    }
}

file static class TestEventNotificationExtensions
{
    public static ITestTransportConfigurationBuilder UseTestTransport(this IEventNotificationReceiver receiver, int parameter)
    {
        return receiver.UseTransport<ITestTransportTypesInjector, ITestTransportConfigurationBuilder>(i => i.CreateWithEventNotificationTypes(new TestEventTransportTypesInjectable(receiver, parameter)));
    }

    public static ITestTransportConfigurationBuilder UseTestTransport<T>(this IEventNotificationReceiver<T> receiver, int parameter)
        where T : class, ITestTransportEventNotification<T>
    {
        receiver.SetConfiguration(new TestEventNotificationTransportReceiverConfiguration { Parameter = parameter });
        return new TestTransportConfigurationBuilder<T>(receiver);
    }

    private sealed class TestEventTransportTypesInjectable(IEventNotificationReceiver receiver, int parameter) : ITestTransportTypesInjectable<ITestTransportConfigurationBuilder>
    {
        public ITestTransportConfigurationBuilder WithInjectedTypes<TEventNotification>()
            where TEventNotification : class, ITestTransportEventNotification<TEventNotification>
        {
            return receiver.For<TEventNotification>().UseTestTransport(parameter);
        }
    }
}

file sealed record TestEventNotificationTransportReceiverConfiguration : IEventNotificationReceiverConfiguration
{
    public required int Parameter { get; set; }

    public int Parameter2 { get; set; }
}
