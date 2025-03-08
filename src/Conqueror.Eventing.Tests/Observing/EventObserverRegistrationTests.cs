using Conqueror.Eventing.Observing;

namespace Conqueror.Eventing.Tests.Observing;

[TestFixture]
public sealed class EventObserverRegistrationTests
{
    [Test]
    [Combinatorial]
    public void GivenRegisteredObservers_WhenCallingRegistry_ReturnsCorrectRegistrations(
        [Values("type", "factory", "instance", "delegate")]
        string registrationMethod)
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddConquerorEventObserver<TestEventObserver>()
                              .AddConquerorEventObserver<TestEvent2Observer>(),
            "factory" => services.AddConquerorEventObserver(_ => new TestEventObserver())
                                 .AddConquerorEventObserver(_ => new TestEvent2Observer()),
            "instance" => services.AddConquerorEventObserver(new TestEventObserver())
                                  .AddConquerorEventObserver(new TestEvent2Observer()),
            "delegate" => services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => throw new NotSupportedException())
                                  .AddConquerorEventObserverDelegate<TestEvent2>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, null),
        };

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IEventTransportRegistry>();
        var expectedRegistrations = new[]
        {
            (typeof(TestEvent), new InProcessEventAttribute()),
            (typeof(TestEvent2), new()),
        };

        var registrations = registry.GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredObserver_WhenRegisteringSameObserverDifferently_OverwritesRegistration(
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
        Func<IServiceProvider, TestEventObserver> factory = _ => new();
        var instance = new TestEventObserver();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorEventObserver<TestEventObserver>(),
                (null, "factory") => services.AddConquerorEventObserver(factory),
                (var l, "type") => services.AddConquerorEventObserver<TestEventObserver>(l.Value),
                (var l, "factory") => services.AddConquerorEventObserver(factory, l.Value),
                (_, "instance") => services.AddConquerorEventObserver(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventObserver)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEvent>)), Is.EqualTo(1));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).ImplementationType, Is.EqualTo(typeof(TestEventObserver)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IEventObserver<TestEvent>>());

        var registry = provider.GetRequiredService<IEventTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestEvent), new InProcessEventAttribute()),
        };

        var registrations = registry.GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredObserver_WhenRegisteringDifferentObserverForSameEventType_RegistersBothObservers(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? firstLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string firstRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? secondLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string secondRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestEventObserver> factory = _ => new();
        Func<IServiceProvider, DuplicateTestEventObserver> duplicateFactory = _ => new();
        var instance = new TestEventObserver();
        var duplicateInstance = new DuplicateTestEventObserver();

        _ = (firstLifetime, firstRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorEventObserver<TestEventObserver>(),
            (null, "factory") => services.AddConquerorEventObserver(factory),
            (var l, "type") => services.AddConquerorEventObserver<TestEventObserver>(l.Value),
            (var l, "factory") => services.AddConquerorEventObserver(factory, l.Value),
            (_, "instance") => services.AddConquerorEventObserver(instance),
            (_, "delegate") => services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(firstRegistrationMethod), firstRegistrationMethod, null),
        };

        _ = (secondLifetime, secondRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorEventObserver<DuplicateTestEventObserver>(),
            (null, "factory") => services.AddConquerorEventObserver(duplicateFactory),
            (var l, "type") => services.AddConquerorEventObserver<DuplicateTestEventObserver>(l.Value),
            (var l, "factory") => services.AddConquerorEventObserver(duplicateFactory, l.Value),
            (_, "instance") => services.AddConquerorEventObserver(duplicateInstance),
            (_, "delegate") => services.AddConquerorEventObserverDelegate<TestEvent>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null),
        };

        if (firstRegistrationMethod != "delegate")
        {
            Assert.That(services.Count(s => s.ServiceType == typeof(TestEventObserver)), Is.EqualTo(1));
        }

        if (secondRegistrationMethod != "delegate")
        {
            Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestEventObserver)), Is.EqualTo(1));
        }

        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEvent>)), Is.EqualTo(1));

        switch (firstLifetime, firstRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).ImplementationType, Is.EqualTo(typeof(TestEventObserver)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserver)).ImplementationInstance, Is.SameAs(instance));
                break;
            case (_, "delegate"):
                Assert.That(services.Where(s => s.ImplementationInstance is IEventObserverInvoker { ObserverType: null }).ToList(), Has.Count.EqualTo(secondRegistrationMethod == "delegate" ? 2 : 1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(firstRegistrationMethod), firstRegistrationMethod, null);
        }

        switch (secondLifetime, secondRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(DuplicateTestEventObserver)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(DuplicateTestEventObserver)).ImplementationType, Is.EqualTo(typeof(DuplicateTestEventObserver)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(DuplicateTestEventObserver)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(DuplicateTestEventObserver)).ImplementationFactory, Is.SameAs(duplicateFactory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(DuplicateTestEventObserver)).ImplementationInstance, Is.SameAs(duplicateInstance));
                break;
            case (_, "delegate"):
                Assert.That(services.Where(s => s.ImplementationInstance is IEventObserverInvoker { ObserverType: null }).ToList(), Has.Count.EqualTo(firstRegistrationMethod == "delegate" ? 2 : 1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(secondRegistrationMethod), secondRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IEventObserver<TestEvent>>());

        var registry = provider.GetRequiredService<IEventTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestEvent), new InProcessEventAttribute()),
        };

        var registrations = registry.GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    public void GivenObserverTypeWithCustomInterface_WhenRegisteringObserverType_RegistersWithPlainAndCustomInterfaceTypes()
    {
        var services = new ServiceCollection().AddConquerorEventObserver<TestEventWithCustomInterfaceObserver>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventWithCustomInterfaceObserver)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ITestEventWithCustomInterfaceObserver)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEventWithCustomInterface>)), Is.EqualTo(1));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEventTransportRegistry>().GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestEventWithCustomInterface), new InProcessEventAttribute())));
    }

    [Test]
    public void GivenObserverTypeWithMultipleObservedEventTypes_WhenRegisteringObserverType_RegistersObserverForAllEventTypes()
    {
        var services = new ServiceCollection().AddConquerorEventObserver<MultiTestEventObserver>();

        Assert.That(services.Count(s => s.ServiceType == typeof(MultiTestEventObserver)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEvent>)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEvent2>)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ITestEventWithCustomInterfaceObserver)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEventWithCustomInterface>)), Is.EqualTo(1));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEventTransportRegistry>().GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestEvent), new InProcessEventAttribute())));
        Assert.That(registrations, Has.One.EqualTo((typeof(TestEvent2), new InProcessEventAttribute())));
        Assert.That(registrations, Has.One.EqualTo((typeof(TestEventWithCustomInterface), new InProcessEventAttribute())));
    }

    [Test]
    public void GivenRegisteredObserverType_WhenRegisteringObserversViaAssemblyScanning_DoesNotOverwriteRegistration()
    {
        var services = new ServiceCollection().AddConquerorEventObserver<TestEventObserverForAssemblyScanning>(ServiceLifetime.Singleton)
                                              .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventObserverForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEventForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserverForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEventTransportRegistry>().GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestEventForAssemblyScanning), new InProcessEventAttribute())));
    }

    [Test]
    public void GivenServiceCollection_WhenRegisteringObserversViaAssemblyScanningMultipleTimes_DoesNotOverwriteRegistrations()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromExecutingAssembly()
                                              .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventObserverForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IEventObserver<TestEventForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestEventObserverForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Transient));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEventTransportRegistry>().GetEventTypesForReceiver<InProcessEventAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestEventForAssemblyScanning), new InProcessEventAttribute())));
    }

    [Test]
    public void GivenObserverWithInvalidInterface_WhenRegisteringObserver_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserver<TestEventObserverWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserver<TestEventObserverWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventObserver(new TestEventObserverWithoutValidInterfaces()));
    }

    private sealed record TestEvent;

    private sealed record TestEvent2;

    public sealed record TestEventWithCustomInterface;

    public sealed record TestEventForAssemblyScanning;

    private sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestEvent2Observer : IEventObserver<TestEvent2>
    {
        public Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestEventObserver : IEventObserver<TestEvent>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public interface ITestEventWithCustomInterfaceObserver : IEventObserver<TestEventWithCustomInterface>;

    private sealed class TestEventWithCustomInterfaceObserver : ITestEventWithCustomInterfaceObserver
    {
        public Task Handle(TestEventWithCustomInterface evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MultiTestEventObserver : IEventObserver<TestEvent>,
                                                  IEventObserver<TestEvent2>,
                                                  ITestEventWithCustomInterfaceObserver
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Handle(TestEventWithCustomInterface evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public sealed class TestEventObserverForAssemblyScanning : IEventObserver<TestEventForAssemblyScanning>
    {
        public Task Handle(TestEventForAssemblyScanning evt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestEventObserverWithoutValidInterfaces : IEventObserver;
}
