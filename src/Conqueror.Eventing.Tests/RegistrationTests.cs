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

        var eventingAssemblies = new[] { typeof(IConquerorEventDispatcher).Assembly, typeof(EventDispatcher).Assembly };
        var conquerorServices = services.Where(d => eventingAssemblies.Contains(d.ServiceType.Assembly))
                                        .Select(d => d.ServiceType)
                                        .Where(t => !t.Name.Contains("Registration"));

        foreach (var serviceType in conquerorServices)
        {
            Assert.That(services.Count(d => d.ServiceType == serviceType), Is.EqualTo(1));
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
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverMiddlewareAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverMiddleware) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventObserverMiddlewareWithoutConfigurationAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventObserverMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventPublisherAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventTransportPublisher) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventPublisherMiddlewareAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventPublisherMiddleware) && d.Lifetime == ServiceLifetime.Transient));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyAddsEventPublisherMiddlewareWithoutConfigurationAsTransient()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType && d.ServiceType == typeof(TestEventPublisherMiddlewareWithoutConfiguration) && d.Lifetime == ServiceLifetime.Transient));
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
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestEventObserverMiddleware)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractEventTransportPublisher)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestEventPublisherMiddleware)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddGenericClasses()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventObserver<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventObserverMiddleware<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventTransportPublisher<>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestEventPublisherMiddleware<>)));
    }

    [Test]
    public void GivenServiceCollection_AddingAllTypesFromAssemblyDoesNotAddPrivateClasses()
    {
        var services = new ServiceCollection().AddConquerorEventingTypesFromAssembly(typeof(RegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventObserver)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventObserverMiddleware)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventTransportPublisher)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestEventPublisherMiddleware)));
    }

    public sealed record TestEvent;

    public sealed record TestEvent2;

    public interface ITestEventObserver : IEventObserver<TestEvent>;

    public interface ITestEventObserver2 : IEventObserver<TestEvent2>;

    public sealed class TestEventObserver : IEventObserver<TestEvent>
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserver2 : IEventObserver<TestEvent>
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithCustomInterface : ITestEventObserver
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithMultiplePlainInterfaces : IEventObserver<TestEvent>, IEventObserver<TestEvent2>
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithMultipleCustomInterfaces : ITestEventObserver, ITestEventObserver2
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverWithMultipleMixedInterfaces : ITestEventObserver, IEventObserver<TestEvent2>
    {
        public Task HandleEvent(TestEvent2 evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public abstract class AbstractTestEventObserver : IEventObserver<TestEvent>
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class GenericTestEventObserver<TEvent> : IEventObserver<TEvent>
        where TEvent : class
    {
        public Task HandleEvent(TEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PrivateTestEventObserver : IEventObserver<TestEvent>
    {
        public Task HandleEvent(TestEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestEventObserverMiddlewareConfiguration;

    public sealed class TestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    public sealed class TestEventObserverMiddlewareWithoutConfiguration : IEventObserverMiddleware
    {
        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    public abstract class AbstractTestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    public sealed class GenericTestEventObserverMiddleware<T> : IEventObserverMiddleware<T>
    {
        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, T> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    private sealed class PrivateTestEventObserverMiddleware : IEventObserverMiddleware<TestEventObserverMiddlewareConfiguration>
    {
        public Task Execute<TEvent>(EventObserverMiddlewareContext<TEvent, TestEventObserverMiddlewareConfiguration> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestEventTransportPublisherConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute;

    public sealed class TestEventTransportPublisher : IConquerorEventTransportPublisher<TestEventTransportPublisherConfigurationAttribute>
    {
        public Task PublishEvent<TEvent>(TEvent evt, TestEventTransportPublisherConfigurationAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            return Task.CompletedTask;
        }
    }

    public abstract class AbstractEventTransportPublisher : IConquerorEventTransportPublisher<TestEventTransportPublisherConfigurationAttribute>
    {
        public Task PublishEvent<TEvent>(TEvent evt, TestEventTransportPublisherConfigurationAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            return Task.CompletedTask;
        }
    }

    public sealed class GenericTestEventTransportPublisher<T> : IConquerorEventTransportPublisher<T>
        where T : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public Task PublishEvent<TEvent>(TEvent evt, T configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            return Task.CompletedTask;
        }
    }

    private sealed class PrivateTestEventTransportPublisher : IConquerorEventTransportPublisher<TestEventTransportPublisherConfigurationAttribute>
    {
        public Task PublishEvent<TEvent>(TEvent evt, TestEventTransportPublisherConfigurationAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
            where TEvent : class
        {
            return Task.CompletedTask;
        }
    }

    public sealed class TestEventPublisherMiddlewareConfiguration;

    public sealed class TestEventPublisherMiddleware : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>
    {
        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    public sealed class TestEventPublisherMiddlewareWithoutConfiguration : IEventPublisherMiddleware
    {
        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    public abstract class AbstractTestEventPublisherMiddleware : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>
    {
        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    public sealed class GenericTestEventPublisherMiddleware<T> : IEventPublisherMiddleware<T>
    {
        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, T> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }

    private sealed class PrivateTestEventPublisherMiddleware : IEventPublisherMiddleware<TestEventPublisherMiddlewareConfiguration>
    {
        public Task Execute<TEvent>(EventPublisherMiddlewareContext<TEvent, TestEventPublisherMiddlewareConfiguration> ctx)
            where TEvent : class =>
            ctx.Next(ctx.Event, ctx.CancellationToken);
    }
}
