using Conqueror.Messaging;

namespace Conqueror.Tests.Messaging;

[TestFixture]
public sealed partial class MessagingRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenRegisteringMultipleHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddConquerorMessageHandler<TestMessageHandler>()
                                              .AddConquerorMessageHandler<TestMessage2Handler>();

        Assert.That(services.Count(d => d.ServiceType == typeof(IMessageClients)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(MessageTransportRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IMessageTransportRegistry)), Is.EqualTo(1));
        Assert.That(services.Count(d => d.ServiceType == typeof(IConquerorContextAccessor)), Is.EqualTo(1));
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromExecutingAssembly_AddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddConquerorMessageHandlersFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    [TestCase(typeof(TestMessageHandler), typeof(TestMessage), typeof(TestMessageResponse))]
    [TestCase(typeof(TestMessageWithoutResponseHandler), typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_AddsMessageHandlerAsTransient(Type handlerType, Type messageType, Type responseType)
    {
        var services = new ServiceCollection().AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IMessageTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (messageType, responseType, new InProcessMessageAttribute()),
        };

        var registrations = registry.GetMessageTypesForTransport<InProcessMessageAttribute>();

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    [TestCase(typeof(TestMessageHandler), typeof(TestMessage), typeof(TestMessageResponse))]
    [TestCase(typeof(TestMessageWithoutResponseHandler), typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssemblyMultipleTimes_AddsMessageHandlerAsTransientOnce(Type handlerType, Type messageType, Type responseType)
    {
        var services = new ServiceCollection().AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly)
                                              .AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IMessageTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (messageType, responseType, new InProcessMessageAttribute()),
        };

        var registrations = registry.GetMessageTypesForTransport<InProcessMessageAttribute>();

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddConquerorMessageHandler<TestMessageHandler>(ServiceLifetime.Singleton)
                                              .AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == typeof(TestMessageHandler)));

        Assert.That(services.Single(d => d.ServiceType == typeof(TestMessageHandler)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IMessageTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestMessage), typeof(TestMessageResponse), new InProcessMessageAttribute()),
        };

        var registrations = registry.GetMessageTypesForTransport<InProcessMessageAttribute>();

        Assert.That(registrations, Is.SupersetOf(expectedRegistrations));
    }

    [Test]
    public void GivenServiceCollectionWithDelegateHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => new());

        Assert.That(() => services.AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly), Throws.InvalidOperationException);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInterfaces()
    {
        var services = new ServiceCollection().AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(IMessageHandler<TestMessage, TestMessageResponse>)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(TestMessage.IHandler)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(IMessageHandler<TestMessageWithoutResponse>)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(TestMessageWithoutResponse.IHandler)), Is.Zero);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInapplicableClasses()
    {
        var services = new ServiceCollection().AddConquerorMessageHandlersFromAssembly(typeof(MessagingRegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(GenericTestMessageHandler<,>)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestMessageWithoutResponseHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MultiTestMessageHandler)));
    }

    [Test]
    public void GivenServiceCollection_WhenAddingInvalidHandlerType_ThrowsInvalidOperationException()
    {
        Assert.That(() => new ServiceCollection().AddConquerorMessageHandler<TestMessage.IHandler>(), Throws.InvalidOperationException);
        Assert.That(() => new ServiceCollection().AddConquerorMessageHandler<ITestMessageHandler>(), Throws.InvalidOperationException);
        Assert.That(() => new ServiceCollection().AddConquerorMessageHandler<AbstractTestMessageHandler>(), Throws.InvalidOperationException);
        Assert.That(() => new ServiceCollection().AddConquerorMessageHandler<MultiTestMessageHandler>(), Throws.InvalidOperationException);
    }

    [Message<TestMessageResponse>]
    public sealed partial record TestMessage;

    public sealed record TestMessageResponse;

    [Message<ExplicitTestMessageResponse>]
    public sealed partial record ExplicitTestMessage;

    public sealed record ExplicitTestMessageResponse;

    [Message<TestMessage2Response>]
    public sealed partial record TestMessage2;

    public sealed record TestMessage2Response;

    [Message]
    public sealed partial record TestMessageWithoutResponse;

    [Message]
    public sealed partial record ExplicitTestMessageWithoutResponse;

    public sealed class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    public sealed class TestMessage2Handler : TestMessage2.IHandler
    {
        public Task<TestMessage2Response> Handle(TestMessage2 message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessage2Response());
    }

    public sealed class ExplicitTestMessageHandler : IMessageHandler<ExplicitTestMessage, ExplicitTestMessageResponse>
    {
        public Task<ExplicitTestMessageResponse> Handle(ExplicitTestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExplicitTestMessageResponse());
    }

    public abstract class AbstractTestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    // in user code this shouldn't even compile, since CreateWithMessageTypes is internal, and the compiler
    // will complain about a non-specific implementation, which is a nice safeguard against users trying to
    // do this
    public sealed class MultiTestMessageHandler : TestMessage.IHandler, TestMessage2.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());

        public Task<TestMessage2Response> Handle(TestMessage2 message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessage2Response());

        public static IDefaultMessageTypesInjector DefaultTypeInjector
            => throw new NotSupportedException();
    }

    public sealed class GenericTestMessageHandler<TM, TR> : IMessageHandler<TM, TR>
        where TM : class, IMessage<TM, TR>
        where TR : new()
    {
        public Task<TR> Handle(TM message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TR());
    }

    public sealed class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class ExplicitTestMessageWithoutResponseHandler : IMessageHandler<ExplicitTestMessageWithoutResponse>
    {
        public Task Handle(ExplicitTestMessageWithoutResponse message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PrivateTestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    private interface ITestMessageHandler : TestMessage.IHandler;
}
