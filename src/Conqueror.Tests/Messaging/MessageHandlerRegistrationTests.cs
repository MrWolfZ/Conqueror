using Conqueror.Messaging;

namespace Conqueror.Tests.Messaging;

[TestFixture]
public sealed partial class MessageHandlerRegistrationTests
{
    [Test]
    public void GivenServiceCollection_WhenRegisteringMultipleHandlers_DoesNotRegisterConquerorTypesMultipleTimes()
    {
        var services = new ServiceCollection().AddConquerorMessageHandler<TestMessageHandler>()
                                              .AddConquerorMessageHandler<TestMessage2Handler>();

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageClients)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MessageTransportRegistry)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageTransportRegistry)));
        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IConquerorContextAccessor)));
    }

    [Test]
    [Combinatorial]
    public void GivenServiceCollection_WhenAddingMessageHandlers_AddsCorrectHandlerRegistrations(
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string registrationMethod)
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddConquerorMessageHandler<TestMessageHandler>()
                              .AddConquerorMessageHandler<TestMessage2Handler>()
                              .AddConquerorMessageHandler<TestMessageWithoutResponseHandler>()
                              .AddConquerorMessageHandler<TestMessageWithoutResponse2Handler>(),
            "factory" => services.AddConquerorMessageHandler(_ => new TestMessageHandler())
                                 .AddConquerorMessageHandler(_ => new TestMessage2Handler())
                                 .AddConquerorMessageHandler(_ => new TestMessageWithoutResponseHandler())
                                 .AddConquerorMessageHandler(_ => new TestMessageWithoutResponse2Handler()),
            "instance" => services.AddConquerorMessageHandler(new TestMessageHandler())
                                  .AddConquerorMessageHandler(new TestMessage2Handler())
                                  .AddConquerorMessageHandler(new TestMessageWithoutResponseHandler())
                                  .AddConquerorMessageHandler(new TestMessageWithoutResponse2Handler()),
            "delegate" => services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => Task.FromResult(new TestMessageResponse()))
                                  .AddConquerorMessageHandlerDelegate<TestMessage2, TestMessage2Response>((_, _, _) => Task.FromResult(new TestMessage2Response()))
                                  .AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => Task.CompletedTask)
                                  .AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse2>((_, _, _) => Task.CompletedTask),
            "sync_delegate" => services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => new())
                                       .AddConquerorMessageHandlerDelegate<TestMessage2, TestMessage2Response>((_, _, _) => new())
                                       .AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => { })
                                       .AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse2>((_, _, _) => { }),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, null),
        };

        Assert.That(services, Has.Exactly(4).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MessageHandlerRegistration)));

        var handlerRegistrations = services.Select(d => d.ImplementationInstance)
                                           .OfType<MessageHandlerRegistration>()
                                           .Select(r => (r.MessageType, r.ResponseType, r.HandlerType))
                                           .ToList();

        var isDelegate = registrationMethod is "delegate" or "sync_delegate";

        var expectedRegistrations = new[]
        {
            (typeof(TestMessage), typeof(TestMessageResponse), isDelegate ? typeof(DelegateMessageHandler<TestMessage, TestMessageResponse>) : typeof(TestMessageHandler)),
            (typeof(TestMessage2), typeof(TestMessage2Response), isDelegate ? typeof(DelegateMessageHandler<TestMessage2, TestMessage2Response>) : typeof(TestMessage2Handler)),
            (typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse), isDelegate ? typeof(DelegateMessageHandler<TestMessageWithoutResponse>) : typeof(TestMessageWithoutResponseHandler)),
            (typeof(TestMessageWithoutResponse2), typeof(UnitMessageResponse), isDelegate ? typeof(DelegateMessageHandler<TestMessageWithoutResponse2>) : typeof(TestMessageWithoutResponse2Handler)),
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
        Func<IServiceProvider, TestMessageHandler> factory = _ => new();
        var instance = new TestMessageHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorMessageHandler<TestMessageHandler>(),
                (null, "factory") => services.AddConquerorMessageHandler(factory),
                (var l, "type") => services.AddConquerorMessageHandler<TestMessageHandler>(l.Value),
                (var l, "factory") => services.AddConquerorMessageHandler(factory, l.Value),
                (_, "instance") => services.AddConquerorMessageHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestMessageHandler)));

        // assert that we do not explicitly register handlers on their interface
        Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageHandler<TestMessage, TestMessageResponse>)));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MessageHandlerRegistration)));

        var handlerServiceDescriptor = services.Single(s => s.ServiceType == typeof(TestMessageHandler));
        var handlerRegistration = services.Select(d => d.ImplementationInstance).OfType<MessageHandlerRegistration>().Single();

        Assert.That(handlerRegistration.MessageType, Is.EqualTo(typeof(TestMessage)));
        Assert.That(handlerRegistration.ResponseType, Is.EqualTo(typeof(TestMessageResponse)));
        Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(TestMessageHandler)));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationType, Is.EqualTo(typeof(TestMessageHandler)));
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
    public void GivenRegisteredHandlerWithoutResponse_WhenRegisteringSameHandlerDifferently_OverwritesRegistration(
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
        Func<IServiceProvider, TestMessageWithoutResponseHandler> factory = _ => new();
        var instance = new TestMessageWithoutResponseHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorMessageHandler<TestMessageWithoutResponseHandler>(),
                (null, "factory") => services.AddConquerorMessageHandler(factory),
                (var l, "type") => services.AddConquerorMessageHandler<TestMessageWithoutResponseHandler>(l.Value),
                (var l, "factory") => services.AddConquerorMessageHandler(factory, l.Value),
                (_, "instance") => services.AddConquerorMessageHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestMessageWithoutResponseHandler)));

        // assert that we do not explicitly register handlers on their interface
        Assert.That(services, Has.Exactly(0).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(IMessageHandler<TestMessageWithoutResponse>)));

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MessageHandlerRegistration)));

        var handlerServiceDescriptor = services.Single(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler));
        var handlerRegistration = services.Select(d => d.ImplementationInstance).OfType<MessageHandlerRegistration>().Single();

        Assert.That(handlerRegistration.MessageType, Is.EqualTo(typeof(TestMessageWithoutResponse)));
        Assert.That(handlerRegistration.ResponseType, Is.EqualTo(typeof(UnitMessageResponse)));
        Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(TestMessageWithoutResponseHandler)));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(handlerServiceDescriptor.Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(handlerServiceDescriptor.ImplementationType, Is.EqualTo(typeof(TestMessageWithoutResponseHandler)));
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
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameMessageType_OverwritesRegistration(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestMessageHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestMessageHandler> duplicateFactory = _ => new();
        var instance = new TestMessageHandler();
        var duplicateInstance = new DuplicateTestMessageHandler();

        _ = (initialLifetime, initialRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorMessageHandler<TestMessageHandler>(),
            (null, "factory") => services.AddConquerorMessageHandler(factory),
            (var l, "type") => services.AddConquerorMessageHandler<TestMessageHandler>(l.Value),
            (var l, "factory") => services.AddConquerorMessageHandler(factory, l.Value),
            (_, "instance") => services.AddConquerorMessageHandler(instance),
            (_, "delegate") => services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => Task.FromResult(new TestMessageResponse())),
            (_, "sync_delegate") => services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => new()),
            _ => throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null),
        };

        _ = (overwrittenLifetime, overwrittenRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorMessageHandler<DuplicateTestMessageHandler>(),
            (null, "factory") => services.AddConquerorMessageHandler(duplicateFactory),
            (var l, "type") => services.AddConquerorMessageHandler<DuplicateTestMessageHandler>(l.Value),
            (var l, "factory") => services.AddConquerorMessageHandler(duplicateFactory, l.Value),
            (_, "instance") => services.AddConquerorMessageHandler(duplicateInstance),
            (_, "delegate") => services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => Task.FromResult(new TestMessageResponse())),
            (_, "sync_delegate") => services.AddConquerorMessageHandlerDelegate<TestMessage, TestMessageResponse>((_, _, _) => new()),
            _ => throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null),
        };

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MessageHandlerRegistration)));

        var handlerRegistration = services.Select(d => d.ImplementationInstance).OfType<MessageHandlerRegistration>().Single();

        Assert.That(handlerRegistration.MessageType, Is.EqualTo(typeof(TestMessage)));
        Assert.That(handlerRegistration.ResponseType, Is.EqualTo(typeof(TestMessageResponse)));

        var expectedInitialLifetime = initialRegistrationMethod is "instance" ? ServiceLifetime.Singleton : initialLifetime ?? ServiceLifetime.Transient;
        Assert.That(services, Has.Exactly(initialRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                                 .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestMessageHandler)
                                                                  && d.Lifetime == expectedInitialLifetime));

        var expectedOverwrittenLifetime = overwrittenRegistrationMethod is "instance" ? ServiceLifetime.Singleton : overwrittenLifetime ?? ServiceLifetime.Transient;
        Assert.That(services, Has.Exactly(overwrittenRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                                 .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(DuplicateTestMessageHandler)
                                                                  && d.Lifetime == expectedOverwrittenLifetime));

        switch (overwrittenRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(DuplicateTestMessageHandler)));
                break;
            case "delegate":
            case "sync_delegate":
                Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(DelegateMessageHandler<TestMessage, TestMessageResponse>)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null);
        }
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandlerWithoutResponse_WhenRegisteringDifferentHandlerForSameMessageType_ThrowsInvalidOperationException(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance", "delegate", "sync_delegate")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestMessageWithoutResponseHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestMessageWithoutResponseHandler> duplicateFactory = _ => new();
        var instance = new TestMessageWithoutResponseHandler();
        var duplicateInstance = new DuplicateTestMessageWithoutResponseHandler();

        _ = (initialLifetime, initialRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorMessageHandler<TestMessageWithoutResponseHandler>(),
            (null, "factory") => services.AddConquerorMessageHandler(factory),
            (var l, "type") => services.AddConquerorMessageHandler<TestMessageWithoutResponseHandler>(l.Value),
            (var l, "factory") => services.AddConquerorMessageHandler(factory, l.Value),
            (_, "instance") => services.AddConquerorMessageHandler(instance),
            (_, "delegate") => services.AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => Task.CompletedTask),
            (_, "sync_delegate") => services.AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => { }),
            _ => throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null),
        };

        _ = (overwrittenLifetime, overwrittenRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorMessageHandler<DuplicateTestMessageWithoutResponseHandler>(),
            (null, "factory") => services.AddConquerorMessageHandler(duplicateFactory),
            (var l, "type") => services.AddConquerorMessageHandler<DuplicateTestMessageWithoutResponseHandler>(l.Value),
            (var l, "factory") => services.AddConquerorMessageHandler(duplicateFactory, l.Value),
            (_, "instance") => services.AddConquerorMessageHandler(duplicateInstance),
            (_, "delegate") => services.AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => Task.CompletedTask),
            (_, "sync_delegate") => services.AddConquerorMessageHandlerDelegate<TestMessageWithoutResponse>((_, _, _) => { }),
            _ => throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null),
        };

        Assert.That(services, Has.Exactly(1).Matches<ServiceDescriptor>(d => d.ServiceType == typeof(MessageHandlerRegistration)));

        var handlerRegistration = services.Select(d => d.ImplementationInstance).OfType<MessageHandlerRegistration>().Single();

        Assert.That(handlerRegistration.MessageType, Is.EqualTo(typeof(TestMessageWithoutResponse)));
        Assert.That(handlerRegistration.ResponseType, Is.EqualTo(typeof(UnitMessageResponse)));

        var expectedInitialLifetime = initialRegistrationMethod is "instance" ? ServiceLifetime.Singleton : initialLifetime ?? ServiceLifetime.Transient;
        Assert.That(services, Has.Exactly(initialRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                                 .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(TestMessageWithoutResponseHandler)
                                                                  && d.Lifetime == expectedInitialLifetime));

        var expectedOverwrittenLifetime = overwrittenRegistrationMethod is "instance" ? ServiceLifetime.Singleton : overwrittenLifetime ?? ServiceLifetime.Transient;
        Assert.That(services, Has.Exactly(overwrittenRegistrationMethod is not "delegate" and not "sync_delegate" ? 1 : 0)
                                 .Matches<ServiceDescriptor>(d => d.ServiceType == typeof(DuplicateTestMessageWithoutResponseHandler)
                                                                  && d.Lifetime == expectedOverwrittenLifetime));

        switch (overwrittenRegistrationMethod)
        {
            case "type":
            case "factory":
            case "instance":
                Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(DuplicateTestMessageWithoutResponseHandler)));
                break;
            case "delegate":
            case "sync_delegate":
                Assert.That(handlerRegistration.HandlerType, Is.EqualTo(typeof(DelegateMessageHandler<TestMessageWithoutResponse>)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null);
        }
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
    private sealed partial record TestMessage;

    private sealed record TestMessageResponse;

    [Message<TestMessage2Response>]
    private sealed partial record TestMessage2;

    private sealed record TestMessage2Response;

    [Message]
    private sealed partial record TestMessageWithoutResponse;

    [Message]
    private sealed partial record TestMessageWithoutResponse2;

    private sealed class TestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestMessage2Handler : TestMessage2.IHandler
    {
        public Task<TestMessage2Response> Handle(TestMessage2 message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestMessageWithoutResponse2Handler : TestMessageWithoutResponse2.IHandler
    {
        public Task Handle(TestMessageWithoutResponse2 message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DuplicateTestMessageWithoutResponseHandler : TestMessageWithoutResponse.IHandler
    {
        public Task Handle(TestMessageWithoutResponse message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private abstract class AbstractTestMessageHandler : TestMessage.IHandler
    {
        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());
    }

    // in user code this shouldn't even compile, since CreateWithMessageTypes is internal, and the compiler
    // will complain about a non-specific implementation, which is a nice safeguard against users trying to
    // do this
    private sealed class MultiTestMessageHandler : TestMessage.IHandler, TestMessage2.IHandler
    {
        public static IDefaultMessageTypesInjector DefaultTypeInjector
            => throw new NotSupportedException();

        public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessageResponse());

        public Task<TestMessage2Response> Handle(TestMessage2 message, CancellationToken cancellationToken = default)
            => Task.FromResult(new TestMessage2Response());
    }

    private interface ITestMessageHandler : TestMessage.IHandler;
}
