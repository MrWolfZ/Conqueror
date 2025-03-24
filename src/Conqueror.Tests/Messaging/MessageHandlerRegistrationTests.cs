using Conqueror.Messaging;

namespace Conqueror.Tests.Messaging;

[TestFixture]
public sealed partial class MessageHandlerRegistrationTests
{
    [Test]
    [Combinatorial]
    public void GivenRegisteredHandlers_WhenCallingRegistry_ReturnsCorrectRegistrations(
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

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IMessageTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestMessage), typeof(TestMessageResponse), new InProcessMessageAttribute()),
            (typeof(TestMessage2), typeof(TestMessage2Response), new()),
            (typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse), new()),
            (typeof(TestMessageWithoutResponse2), typeof(UnitMessageResponse), new()),
        };

        var registrations = registry.GetMessageTypesForTransport<InProcessMessageAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
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

        Assert.That(services.Count(s => s.ServiceType == typeof(TestMessageHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IMessageHandler<TestMessage, TestMessageResponse>)), Is.EqualTo(0));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageHandler)).ImplementationType, Is.EqualTo(typeof(TestMessageHandler)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageHandler)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageHandler)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IMessageTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestMessage), typeof(TestMessageResponse), new InProcessMessageAttribute()),
        };

        var registrations = registry.GetMessageTypesForTransport<InProcessMessageAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
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

        Assert.That(services.Count(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(IMessageHandler<TestMessageWithoutResponse>)), Is.EqualTo(0));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler)).ImplementationType, Is.EqualTo(typeof(TestMessageWithoutResponseHandler)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestMessageWithoutResponseHandler)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<IMessageTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestMessageWithoutResponse), typeof(UnitMessageResponse), new InProcessMessageAttribute()),
        };

        var registrations = registry.GetMessageTypesForTransport<InProcessMessageAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameMessageType_ThrowsInvalidOperationException(
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

        _ = Assert.Throws<InvalidOperationException>(() =>
        {
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
        });
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

        _ = Assert.Throws<InvalidOperationException>(() =>
        {
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
        });
    }

    private sealed partial record TestMessage : IMessage<TestMessageResponse>;

    private sealed record TestMessageResponse;

    private sealed partial record TestMessage2 : IMessage<TestMessage2Response>;

    private sealed record TestMessage2Response;

    private sealed partial record TestMessageWithoutResponse : IMessage;

    private sealed partial record TestMessageWithoutResponse2 : IMessage;

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
}
