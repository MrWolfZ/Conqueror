using Conqueror.Eventing.Observing;
using Conqueror.Eventing.Publishing;

namespace Conqueror.Eventing.Tests;

[TestFixture]
public sealed class RegistrationTests
{
    [Test]
    public void GivenServiceCollectionWithConquerorAlreadyRegistered_DoesNotRegisterConquerorTypesAgain()
    {
        var services = new ServiceCollection().AddConquerorEventObserver<TestEventObserver>()
                                              .AddConquerorEventObserver<TestEventObserver2>();

        var eventingAssemblies = new[] { typeof(IEventDispatcher).Assembly, typeof(EventDispatcher).Assembly };
        var conquerorServices = services.Where(d => eventingAssemblies.Contains(d.ServiceType.Assembly))
                                        .Select(d => d.ServiceType)
                                        .Distinct()
                                        .Where(t => t != typeof(IEventObserverInvoker));

        foreach (var serviceType in conquerorServices)
        {
            Assert.That(services, Has.Exactly(1).Matches((ServiceDescriptor d) => d.ServiceType == serviceType));
        }
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromExecutingAssemblyAddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddConquerorEventingTypesFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithPlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserver) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithCustomInterface) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithMultiplePlainInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultiplePlainInterfaces) && d.Lifetime == ServiceLifetime.Transient));
        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultiplePlainInterfaces) && d.Lifetime == ServiceLifetime.Transient), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithMultipleCustomInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleCustomInterfaces) && d.Lifetime == ServiceLifetime.Transient));
        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleCustomInterfaces) && d.Lifetime == ServiceLifetime.Transient), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverWithMultipleMixedInterfaceAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleMixedInterfaces) && d.Lifetime == ServiceLifetime.Transient));
        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverWithMultipleMixedInterfaces) && d.Lifetime == ServiceLifetime.Transient), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventPublisherAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventTransportPublisher) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollectionWithObserverAlreadyRegistered_AddingAllTypesFromAssemblyDoesNotAddObserverAgain()
    {
        var services = new ServiceCollection().AddConquerorEventObserver<TestEventObserver>(ServiceLifetime.Singleton)
                                              .AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserver)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsCustomEventObserverInterfaces()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(ITestEventObserver)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddAbstractClasses()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestEventObserver)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractEventTransportPublisher)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventObserver<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventTransportPublisher<>)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddPrivateClasses()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventObserver)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventTransportPublisher)));
    }

    public sealed record TestEvent;

    public sealed record TestEvent2;

    public interface ITestEventObserver : IEventObserver<TestEvent>;

    public interface ITestEventObserver2 : IEventObserver<TestEvent2>;

    public sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserver2 : IEventObserver<TestEvent>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithCustomInterface : ITestEventObserver
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithMultiplePlainInterfaces : IEventObserver<TestEvent>, IEventObserver<TestEvent2>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithMultipleCustomInterfaces : ITestEventObserver, ITestEventObserver2
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithMultipleMixedInterfaces : ITestEventObserver, IEventObserver<TestEvent2>
    {
        public Task Handle(TestEvent2 evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public abstract class AbstractTestEventObserver : IEventObserver<TestEvent>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class GenericTestEventObserver<TEvent> : IEventObserver<TEvent>
        where TEvent : class
    {
        public Task Handle(TEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PrivateTestEventObserver : IEventObserver<TestEvent>
    {
        public Task Handle(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverMiddlewareConfiguration;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransportPublisherConfigurationAttribute() : EventTransportAttribute(nameof(TestEventTransportPublisherConfigurationAttribute));

    public sealed class TestEventTransportPublisher : IEventTransportPublisher<TestEventTransportPublisherConfigurationAttribute>
    {
        public Task PublishEvent(object evt, TestEventTransportPublisherConfigurationAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public abstract class AbstractEventTransportPublisher : IEventTransportPublisher<TestEventTransportPublisherConfigurationAttribute>
    {
        public Task PublishEvent(object evt, TestEventTransportPublisherConfigurationAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public sealed class GenericTestEventTransportPublisher<T> : IEventTransportPublisher<T>
        where T : EventTransportAttribute
    {
        public Task PublishEvent(object evt, T attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class PrivateTestEventTransportPublisher : IEventTransportPublisher<TestEventTransportPublisherConfigurationAttribute>
    {
        public Task PublishEvent(object evt, TestEventTransportPublisherConfigurationAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
