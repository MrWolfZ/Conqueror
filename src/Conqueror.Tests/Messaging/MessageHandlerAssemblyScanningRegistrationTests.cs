using Conqueror.Messaging;

namespace Conqueror.Tests.Messaging;

[TestFixture]
public partial class MessageHandlerAssemblyScanningRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromExecutingAssembly_AddsSameTypesAsIfAssemblyWasSpecifiedExplicitly()
    {
        var services1 = new ServiceCollection().AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);
        var services2 = new ServiceCollection().AddMessageHandlersFromExecutingAssembly();

        Assert.That(services2, Has.Count.EqualTo(services1.Count));
        Assert.That(services1.Select(d => d.ServiceType), Is.EquivalentTo(services2.Select(d => d.ServiceType)));
    }

    [Test]
    [TestCase(typeof(TestMessageHandler), typeof(TestMessage), typeof(TestMessageResponse))]
    [TestCase(typeof(InternalTestMessageHandler), typeof(InternalTestMessage), typeof(TestMessageResponse))]
    [TestCase(typeof(InternalTopLevelTestMessageHandler), typeof(InternalTopLevelTestMessage), typeof(TestMessageResponse))]
    [TestCase(typeof(TestMessageWithoutResponseHandler), typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_AddsMessageHandlerAsTransient(Type handlerType, Type messageType, Type responseType)
    {
        var services = new ServiceCollection().AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                             && r.MessageType == messageType
                                                                             && r.ResponseType == responseType
                                                                             && r.HandlerType == handlerType));
    }

    [Test]
    [TestCase(typeof(TestMessageHandler), typeof(TestMessage), typeof(TestMessageResponse))]
    [TestCase(typeof(TestMessageWithoutResponseHandler), typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse))]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssemblyMultipleTimes_AddsMessageHandlerAsTransientOnce(Type handlerType, Type messageType, Type responseType)
    {
        var services = new ServiceCollection().AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly)
                                              .AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == handlerType
                                                                             && d.Lifetime == ServiceLifetime.Transient));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                             && r.MessageType == messageType
                                                                             && r.ResponseType == responseType
                                                                             && r.HandlerType == handlerType));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddMessageHandler<TestMessageHandler>(ServiceLifetime.Singleton)
                                              .AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == typeof(TestMessageHandler)));

        Assert.That(services.Single(d => d.ServiceType == typeof(TestMessageHandler)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                             && r.MessageType == typeof(TestMessage)
                                                                             && r.ResponseType == typeof(TestMessageResponse)
                                                                             && r.HandlerType == typeof(TestMessageHandler)));
    }

    [Test]
    public void GivenServiceCollectionWithDelegateHandlerAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddMessageHandlerDelegate(TestMessage.T, (_, _, _) => new())
                                              .AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                             && r.MessageType == typeof(TestMessage)
                                                                             && r.ResponseType == typeof(TestMessageResponse)));
    }

    [Test]
    public void GivenServiceCollectionWithHandlerWithoutResponseAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddMessageHandler<TestMessageWithoutResponseHandler>(ServiceLifetime.Singleton)
                                              .AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationType == d.ServiceType
                                                                             && d.ServiceType == typeof(TestMessageWithoutResponseHandler)));

        Assert.That(services.Single(d => d.ServiceType == typeof(TestMessageWithoutResponseHandler)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                             && r.MessageType == typeof(TestMessageWithoutResponse)
                                                                             && r.ResponseType == typeof(UnitMessageResponse)
                                                                             && r.HandlerType == typeof(TestMessageWithoutResponseHandler)));
    }

    [Test]
    public void GivenServiceCollectionWithDelegateHandlerWithoutResponseAlreadyRegistered_WhenAddingAllHandlersFromAssembly_DoesNotAddHandlerAgain()
    {
        var services = new ServiceCollection().AddMessageHandlerDelegate(TestMessageWithoutResponse.T, (_, _, _) => Task.CompletedTask)
                                              .AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                             && r.MessageType == typeof(TestMessageWithoutResponse)
                                                                             && r.ResponseType == typeof(UnitMessageResponse)));
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInterfaces()
    {
        var services = new ServiceCollection().AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services.Count(d => d.ServiceType == typeof(IMessageHandler<TestMessage, TestMessageResponse, TestMessage.IHandler, TestMessage.IHandler.Proxy, TestMessage.IPipeline, TestMessage.IPipeline.Proxy>)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(TestMessage.IHandler)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(IMessageHandler<TestMessageWithoutResponse, UnitMessageResponse, TestMessageWithoutResponse.IHandler, TestMessageWithoutResponse.IHandler.Proxy, TestMessageWithoutResponse.IPipeline, TestMessageWithoutResponse.IPipeline.Proxy>)), Is.Zero);
        Assert.That(services.Count(d => d.ServiceType == typeof(TestMessageWithoutResponse.IHandler)), Is.Zero);
    }

    [Test]
    public void GivenServiceCollection_WhenAddingAllHandlersFromAssembly_DoesNotAddInapplicableClasses()
    {
        var services = new ServiceCollection().AddMessageHandlersFromAssembly(typeof(MessageHandlerAssemblyScanningRegistrationTests).Assembly);

        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(AbstractTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(PrivateTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ProtectedTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestMessageHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(ExplicitTestMessageWithoutResponseHandler)));
        Assert.That(services, Has.None.Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MultiTestMessageHandler)));
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

    [Message<TestMessageResponse>]
    internal sealed partial record InternalTestMessage;

    [Message<TestMessageResponse>]
    private sealed partial record PrivateTestMessage;

    [Message<TestMessageResponse>]
    protected sealed partial record ProtectedTestMessage;

    public sealed partial class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    public sealed class ExplicitTestMessageHandler : IMessageHandler<ExplicitTestMessage, ExplicitTestMessageResponse, ExplicitTestMessage.IHandler, ExplicitTestMessage.IHandler.Proxy, ExplicitTestMessage.IPipeline, ExplicitTestMessage.IPipeline.Proxy>
    {
        public Task<ExplicitTestMessageResponse> Handle(ExplicitTestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new ExplicitTestMessageResponse());

        static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors() => [];
    }

    public abstract partial class AbstractTestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    public sealed partial class MultiTestMessageHandler : TestMessage.IHandler, TestMessage2.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());

        public Task<TestMessage2Response> Handle(TestMessage2 message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessage2Response());
    }

    public sealed partial class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public sealed class ExplicitTestMessageWithoutResponseHandler : IMessageHandler<ExplicitTestMessageWithoutResponse, UnitMessageResponse, ExplicitTestMessageWithoutResponse.IHandler, ExplicitTestMessageWithoutResponse.IHandler.Proxy, ExplicitTestMessageWithoutResponse.IPipeline, ExplicitTestMessageWithoutResponse.IPipeline.Proxy>
    {
        public Task Handle(ExplicitTestMessageWithoutResponse message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors() => [];
    }

    protected sealed partial class ProtectedTestMessageHandler : ProtectedTestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(ProtectedTestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    internal sealed partial class InternalTestMessageHandler : InternalTestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(InternalTestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    private sealed partial class PrivateTestMessageHandler : PrivateTestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(PrivateTestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }
}

[Message<MessageHandlerAssemblyScanningRegistrationTests.TestMessageResponse>]
internal sealed partial record InternalTopLevelTestMessage;

internal sealed partial class InternalTopLevelTestMessageHandler : InternalTopLevelTestMessage.IHandler
{
    public Task<MessageHandlerAssemblyScanningRegistrationTests.TestMessageResponse> Handle(InternalTopLevelTestMessage message, CancellationToken cancellationToken = default)
        => Task.FromResult(new MessageHandlerAssemblyScanningRegistrationTests.TestMessageResponse());
}
