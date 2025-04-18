using Conqueror.Eventing;

namespace Conqueror.Tests.Eventing;

[TestFixture]
public sealed partial class EventNotificationHandlerRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenRegisteringMultipleHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddEventNotificationHandler<TestEventNotificationHandler>()
                                              .AddEventNotificationHandler<TestEventNotification2Handler>();

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IEventNotificationPublishers)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IEventNotificationIdFactory)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(EventNotificationTransportRegistry)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IEventNotificationTransportRegistry)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(InProcessEventNotificationReceiver)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IConquerorContextAccessor)));
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingEventNotificationHandlers_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance", "delegate", "sync_delegate", "explicit_delegate", "explicit_sync_delegate")]
        string registrationMethod)
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddEventNotificationHandler<TestEventNotificationHandler>()
                              .AddEventNotificationHandler<TestEventNotification2Handler>(),
            "factory" => services.AddEventNotificationHandler(_ => new TestEventNotificationHandler())
                                 .AddEventNotificationHandler(_ => new TestEventNotification2Handler()),
            "instance" => services.AddEventNotificationHandler(new TestEventNotificationHandler())
                                  .AddEventNotificationHandler(new TestEventNotification2Handler()),
            "delegate" => services.AddEventNotificationHandlerDelegate(TestEventNotification.T, (_, _, _) => Task.CompletedTask)
                                  .AddEventNotificationHandlerDelegate(TestEventNotification2.T, (_, _, _) => Task.CompletedTask),
            "sync_delegate" => services.AddEventNotificationHandlerDelegate(TestEventNotification.T, (_, _, _) => { })
                                       .AddEventNotificationHandlerDelegate(TestEventNotification2.T, (_, _, _) => { }),
            "explicit_delegate" => services.AddEventNotificationHandlerDelegate<TestEventNotification>((_, _, _) => Task.CompletedTask)
                                           .AddEventNotificationHandlerDelegate<TestEventNotification2>((_, _, _) => Task.CompletedTask),
            "explicit_sync_delegate" => services.AddEventNotificationHandlerDelegate<TestEventNotification>((_, _, _) => { })
                                                .AddEventNotificationHandlerDelegate<TestEventNotification2>((_, _, _) => { }),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, null),
        };

        Assert.That(services, Has.Exactly(2).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(EventNotificationHandlerRegistration)));

        var handlerRegistrations = services.Select(d => d.ImplementationInstance)
                                           .OfType<EventNotificationHandlerRegistration>()
                                           .Select(r => (r.EventNotificationType, r.HandlerType, r.HandlerFn is not null))
                                           .ToList();

        var isDelegate = registrationMethod is "delegate" or "sync_delegate" or "explicit_delegate" or "explicit_sync_delegate";

        var expectedRegistrations = new[]
        {
            (typeof(TestEventNotification), isDelegate ? null : typeof(TestEventNotificationHandler), isDelegate),
            (typeof(TestEventNotification2), isDelegate ? null : typeof(TestEventNotification2Handler), isDelegate),
        };

        Assert.That(handlerRegistrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingEventNotificationHandlerForMultipleNotificationTypes_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance")]
        string registrationMethod)
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddEventNotificationHandler<MultiTestEventNotificationHandler>(),
            "factory" => services.AddEventNotificationHandler(_ => new MultiTestEventNotificationHandler()),
            "instance" => services.AddEventNotificationHandler(new MultiTestEventNotificationHandler()),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, null),
        };

        Assert.That(services, Has.Exactly(2).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(EventNotificationHandlerRegistration)));

        var handlerRegistrations = services.Select(d => d.ImplementationInstance)
                                           .OfType<EventNotificationHandlerRegistration>()
                                           .Select(r => (r.EventNotificationType, r.HandlerType))
                                           .ToList();

        var expectedRegistrations = new[]
        {
            (typeof(TestEventNotification), typeof(MultiTestEventNotificationHandler)),
            (typeof(TestEventNotification2), typeof(MultiTestEventNotificationHandler)),
        };

        Assert.That(handlerRegistrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringSameHandlerDifferently_OverwritesRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestEventNotificationHandler> factory = _ => new();
        var instance = new TestEventNotificationHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddEventNotificationHandler<TestEventNotificationHandler>(),
                (null, "factory") => services.AddEventNotificationHandler(factory),
                (var l, "type") => services.AddEventNotificationHandler<TestEventNotificationHandler>(l.Value),
                (var l, "factory") => services.AddEventNotificationHandler(factory, l.Value),
                (_, "instance") => services.AddEventNotificationHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestEventNotificationHandler)));

        // assert that we do not explicitly register handlers on their interface
        Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IEventNotificationHandler<TestEventNotification>)));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(EventNotificationHandlerRegistration)));

        var handlerServiceDescriptor = services.Single(s => s.ServiceType == typeof(TestEventNotificationHandler));
        var handlerRegistration = services.Select(d => d.ImplementationInstance).OfType<EventNotificationHandlerRegistration>().Single();

        Assert.That(handlerRegistration.EventNotificationType, Is.EqualTo(typeof(TestEventNotification)));
        Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(TestEventNotificationHandler)));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationType, Is.EqualTo(typeof(TestEventNotificationHandler)));
                break;
            case (var l, "factory"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(handlerServiceDescriptor.ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameEventNotificationType_AddsSeparateRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? firstLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string firstRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? secondLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string secondRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestEventNotificationHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestEventNotificationHandler> duplicateFactory = _ => new();
        var instance = new TestEventNotificationHandler();
        var duplicateInstance = new DuplicateTestEventNotificationHandler();

        _ = (firstLifetime, firstRegistrationMethod) switch
        {
            (null, "type") => services.AddEventNotificationHandler<TestEventNotificationHandler>(),
            (null, "factory") => services.AddEventNotificationHandler(factory),
            (var l, "type") => services.AddEventNotificationHandler<TestEventNotificationHandler>(l.Value),
            (var l, "factory") => services.AddEventNotificationHandler(factory, l.Value),
            (_, "instance") => services.AddEventNotificationHandler(instance),
            (_, "delegate") => services.AddEventNotificationHandlerDelegate<TestEventNotification>((_, _, _) => Task.CompletedTask),
            (_, "sync_delegate") => services.AddEventNotificationHandlerDelegate((EventNotificationHandlerFn<TestEventNotification>)((_, _, _) => Task.CompletedTask)),
            _ => throw new ArgumentOutOfRangeException(nameof(firstRegistrationMethod), firstRegistrationMethod, null),
        };

        _ = (secondLifetime, secondRegistrationMethod) switch
        {
            (null, "type") => services.AddEventNotificationHandler<DuplicateTestEventNotificationHandler>(),
            (null, "factory") => services.AddEventNotificationHandler(duplicateFactory),
            (var l, "type") => services.AddEventNotificationHandler<DuplicateTestEventNotificationHandler>(l.Value),
            (var l, "factory") => services.AddEventNotificationHandler(duplicateFactory, l.Value),
            (_, "instance") => services.AddEventNotificationHandler(duplicateInstance),
            (_, "delegate") => services.AddEventNotificationHandlerDelegate<TestEventNotification>((_, _, _) => Task.CompletedTask),
            (_, "sync_delegate") => services.AddEventNotificationHandlerDelegate<TestEventNotification>((_, _, _) => { }),
            _ => throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null),
        };

        Assert.That(services, Has.Exactly(2).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(EventNotificationHandlerRegistration)));

        var handlerRegistration1 = services.Select(d => d.ImplementationInstance).OfType<EventNotificationHandlerRegistration>().First();
        var handlerRegistration2 = services.Select(d => d.ImplementationInstance).OfType<EventNotificationHandlerRegistration>().Last();

        Assert.That(handlerRegistration1.EventNotificationType, Is.EqualTo(typeof(TestEventNotification)));
        Assert.That(handlerRegistration2.EventNotificationType, Is.EqualTo(typeof(TestEventNotification)));

        var expectedFirstLifetime = firstRegistrationMethod is "instance" ? ServiceLifetime.Singleton : firstLifetime ?? ServiceLifetime.Transient;
        Assert.That(services, Has.Exactly(firstRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                                 .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestEventNotificationHandler)
                                                                  && d.Lifetime == expectedFirstLifetime));

        var expectedSecondLifetime = secondRegistrationMethod is "instance" ? ServiceLifetime.Singleton : secondLifetime ?? ServiceLifetime.Transient;
        Assert.That(services, Has.Exactly(secondRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                                 .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(DuplicateTestEventNotificationHandler)
                                                                  && d.Lifetime == expectedSecondLifetime));

        switch (firstRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration1.HandlerType, Is.EqualTo(typeof(TestEventNotificationHandler)));
                Assert.That(handlerRegistration1.HandlerFn, Is.Null);
                break;
            case "delegate":
            case "sync_delegate":
                Assert.That(handlerRegistration1.HandlerType, Is.Null);
                Assert.That(handlerRegistration1.HandlerFn, Is.Not.Null);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null);
        }

        switch (secondRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration2.HandlerType, Is.EqualTo(typeof(DuplicateTestEventNotificationHandler)));
                Assert.That(handlerRegistration2.HandlerFn, Is.Null);
                break;
            case "delegate":
            case "sync_delegate":
                Assert.That(handlerRegistration2.HandlerType, Is.Null);
                Assert.That(handlerRegistration2.HandlerFn, Is.Not.Null);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IEventNotificationPublishers>().For(TestEventNotification.T);

        Assert.That(() => handler.Handle(new()), Throws.Nothing);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingInvalidHandlerType_ThrowsInvalidOperationException()
    {
        Assert.That(() => new ServiceCollection().AddEventNotificationHandler<TestEventNotification.IHandler>(),
                    Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class"));

        Assert.That(() => new ServiceCollection().AddEventNotificationHandler<ITestEventNotificationHandler>(),
                    Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class"));

        Assert.That(() => new ServiceCollection().AddEventNotificationHandler<AbstractTestEventNotificationHandler>(),
                    Throws.InvalidOperationException.With.Message.Match("must not be an interface or abstract class"));
    }

    [EventNotification]
    private sealed partial record TestEventNotification;

    [EventNotification]
    private sealed partial record TestEventNotification2;

    private sealed class TestEventNotificationHandler : TestEventNotification.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestEventNotification2Handler : TestEventNotification2.IHandler
    {
        public Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DuplicateTestEventNotificationHandler : TestEventNotification.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MultiTestEventNotificationHandler : TestEventNotification.IHandler, TestEventNotification2.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEventNotification2 notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private abstract class AbstractTestEventNotificationHandler : TestEventNotification.IHandler
    {
        public Task Handle(TestEventNotification notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private interface ITestEventNotificationHandler : TestEventNotification.IHandler;
}
