using Conqueror.Eventing;

namespace Conqueror.Tests.Eventing;

[TestFixture]
public partial class EventNotificationHandlerAssemblyScanningRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromExecutingAssembly_AddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddEventNotificationHandlersFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    [TestCase(typeof(TestEventNotificationHandler), typeof(TestEventNotification))]
    [TestCase(typeof(InternalTestEventNotificationHandler), typeof(InternalTestEventNotification))]
    [TestCase(typeof(InternalTopLevelTestEventNotificationHandler), typeof(InternalTopLevelTestEventNotification))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_AddsEventNotificationHandlerAsTransient(Type handlerType, Type notificationType)
    {
        var services = new ServiceCollection().AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is EventNotificationHandlerRegistration r
                                                                             && r.EventNotificationType == notificationType
                                                                             && r.HandlerType == handlerType));
    }

    [Test]
    [TestCase(typeof(TestEventNotificationHandler), new[] { typeof(TestEventNotification) })]
    [TestCase(typeof(MultiTestEventNotificationHandler), new[] { typeof(TestEventNotification), typeof(TestEventNotification2) })]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssemblyMultipleTimes_AddsEventNotificationHandlerAsTransientOnce(Type handlerType, Type[] notificationTypes)
    {
        var services = new ServiceCollection().AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly)
                                              .AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        Assert.That(services, Has.Exactly(notificationTypes.Length).Matches<ServiceDescriptor>(d => d.ImplementationInstance is EventNotificationHandlerRegistration r
                                                                                                    && notificationTypes.Any(t => t == r.EventNotificationType)
                                                                                                    && r.HandlerType == handlerType));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddEventNotificationHandler<TestEventNotificationHandler>(ServiceLifetime.Singleton)
                                              .AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == typeof(TestEventNotificationHandler)));

        Assert.That(services.Single(d => d.ServiceType == typeof(TestEventNotificationHandler)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is EventNotificationHandlerRegistration r
                                                                             && r.EventNotificationType == typeof(TestEventNotification)
                                                                             && r.HandlerType == typeof(TestEventNotificationHandler)));
    }

    [Test]
    public void GivenServiceCollectionWithDelegateHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_AddsOtherHandlers()
    {
        var services = new ServiceCollection().AddEventNotificationHandlerDelegate(TestEventNotification.T, (_, _, _) => { })
                                              .AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is EventNotificationHandlerRegistration r
                                                                             && r.EventNotificationType == typeof(TestEventNotification)
                                                                             && r.HandlerFn is not null));

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationInstance is EventNotificationHandlerRegistration r
                                                                       && r.EventNotificationType == typeof(TestEventNotification)
                                                                       && r.HandlerFn is null));
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInterfaces()
    {
        var services = new ServiceCollection().AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(IEventNotificationHandler<TestEventNotification, TestEventNotification.IHandler>)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(TestEventNotification.IHandler)), Is.Zero);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInapplicableClasses()
    {
        var services = new ServiceCollection().AddEventNotificationHandlersFromAssembly(typeof(EventNotificationHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestEventNotificationHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventNotificationHandler<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventNotificationHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ProtectedTestEventNotificationHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestEventNotificationHandler)));
    }

    [EventNotification]
    public sealed partial record TestEventNotification;

    [EventNotification]
    public sealed partial record ExplicitTestEventNotification;

    [EventNotification]
    public sealed partial record TestEventNotification2;

    [EventNotification]
    internal sealed partial record InternalTestEventNotification;

    [EventNotification]
    private sealed partial record PrivateTestEventNotification;

    [EventNotification]
    protected sealed partial record ProtectedTestEventNotification;

    public sealed class TestEventNotificationHandler : TestEventNotification.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class ExplicitTestEventNotificationHandler : IEventNotificationHandler<ExplicitTestEventNotification, ExplicitTestEventNotificationHandler>
    {
        public Task Handle(ExplicitTestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public abstract class AbstractTestEventNotificationHandler : TestEventNotification.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class MultiTestEventNotificationHandler : TestEventNotification.IHandler, TestEventNotification2.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class GenericTestEventNotificationHandler<TM> : IEventNotificationHandler<TM, GenericTestEventNotificationHandler<TM>>
        where TM : class, IEventNotification<TM>
    {
        public Task Handle(TM notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    protected sealed class ProtectedTestEventNotificationHandler : ProtectedTestEventNotification.IHandler
    {
        public Task Handle(ProtectedTestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    internal sealed class InternalTestEventNotificationHandler : InternalTestEventNotification.IHandler
    {
        public Task Handle(InternalTestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PrivateTestEventNotificationHandler : PrivateTestEventNotification.IHandler
    {
        public Task Handle(PrivateTestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

[EventNotification]
internal sealed partial record InternalTopLevelTestEventNotification;

internal sealed class InternalTopLevelTestEventNotificationHandler : InternalTopLevelTestEventNotification.IHandler
{
    public Task Handle(InternalTopLevelTestEventNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
