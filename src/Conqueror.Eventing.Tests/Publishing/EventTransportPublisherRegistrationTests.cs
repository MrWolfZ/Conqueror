using Conqueror.Eventing.Publishing;

namespace Conqueror.Eventing.Tests.Publishing;

public sealed class EventTransportPublisherRegistrationTests
{
    [Test]
    [Combinatorial]
    public void GivenRegisteredPublisher_WhenRegisteringSamePublisherDifferently_OverwritesRegistration(
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
        Func<IServiceProvider, TestEventTransportPublisher> factory = _ => new();
        var instance = new TestEventTransportPublisher();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>(),
                (null, "factory") => services.AddConquerorEventTransportPublisher(factory),
                (var l, "type") => services.AddConquerorEventTransportPublisher<TestEventTransportPublisher>(l.Value),
                (var l, "factory") => services.AddConquerorEventTransportPublisher(factory, l.Value),
                (_, "instance") => services.AddConquerorEventTransportPublisher(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventTransportPublisher)), Is.EqualTo(1));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportPublisher)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportPublisher)).ImplementationType, Is.EqualTo(typeof(TestEventTransportPublisher)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportPublisher)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportPublisher)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportPublisher)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<TestEventTransportPublisher>());

        var registrations = provider.GetRequiredService<IEnumerable<EventTransportPublisherRegistration>>();

        var expectedRegistrations = new[]
        {
            new EventTransportPublisherRegistration(typeof(TestEventTransportPublisher), typeof(TestEventTransportAttribute)),
        };

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    public void GivenPublisherForMultipleTransports_WhenRegisteringPublisher_CreatesRegistrationsForAllTransports()
    {
        var services = new ServiceCollection().AddConquerorEventTransportPublisher<MultiTestEventTransportPublisher>();

        Assert.That(services.Count(s => s.ServiceType == typeof(MultiTestEventTransportPublisher)), Is.EqualTo(1));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEnumerable<EventTransportPublisherRegistration>>();

        var expectedRegistrations = new[]
        {
            new EventTransportPublisherRegistration(typeof(MultiTestEventTransportPublisher), typeof(TestEventTransportAttribute)),
            new EventTransportPublisherRegistration(typeof(MultiTestEventTransportPublisher), typeof(TestEventTransport2Attribute)),
        };

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    public void GivenRegisteredPublisherType_WhenRegisteringPublishersViaAssemblyScanning_DoesNotOverwriteRegistration()
    {
        var services = new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportForAssemblyScanningPublisher>(ServiceLifetime.Singleton)
                                              .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventTransportForAssemblyScanningPublisher)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportForAssemblyScanningPublisher)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEnumerable<EventTransportPublisherRegistration>>();

        var expectedRegistrations = new[]
        {
            new EventTransportPublisherRegistration(typeof(TestEventTransportForAssemblyScanningPublisher), typeof(TestEventTransportForAssemblyScanningAttribute)),
        };

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    public void GivenServiceCollection_WhenRegisteringPublishersViaAssemblyScanningMultipleTimes_DoesNotOverwriteRegistrations()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromExecutingAssembly()
                                              .AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestEventTransportForAssemblyScanningPublisher)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestEventTransportForAssemblyScanningPublisher)).Lifetime, Is.EqualTo(ServiceLifetime.Transient));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IEnumerable<EventTransportPublisherRegistration>>();

        var expectedRegistrations = new[]
        {
            new EventTransportPublisherRegistration(typeof(TestEventTransportForAssemblyScanningPublisher), typeof(TestEventTransportForAssemblyScanningAttribute)),
        };

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    public void GivenPublisherWithInvalidInterface_WhenRegisteringPublisher_ThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher<TestEventTransportPublisherWithoutValidInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorEventTransportPublisher(new TestEventTransportPublisherWithoutValidInterfaces()));
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransportAttribute() : EventTransportAttribute(nameof(TestEventTransportAttribute));

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestEventTransport2Attribute() : EventTransportAttribute(nameof(TestEventTransport2Attribute));

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransportForAssemblyScanningAttribute() : EventTransportAttribute(nameof(TestEventTransportForAssemblyScanningAttribute));

    private sealed class TestEventTransportPublisher : IEventTransportPublisher<TestEventTransportAttribute>
    {
        public Task PublishEvent(object evt, TestEventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MultiTestEventTransportPublisher : IEventTransportPublisher<TestEventTransportAttribute>,
                                                            IEventTransportPublisher<TestEventTransport2Attribute>
    {
        public Task PublishEvent(object evt, TestEventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task PublishEvent(object evt, TestEventTransport2Attribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class TestEventTransportForAssemblyScanningPublisher : IEventTransportPublisher<TestEventTransportForAssemblyScanningAttribute>
    {
        public Task PublishEvent(object evt, TestEventTransportForAssemblyScanningAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestEventTransportPublisherWithoutValidInterfaces : IEventTransportPublisher;
}
