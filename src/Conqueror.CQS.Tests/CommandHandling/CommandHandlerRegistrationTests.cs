using Conqueror.CQS.CommandHandling;

namespace Conqueror.CQS.Tests.CommandHandling;

[TestFixture]
public sealed class CommandHandlerRegistrationTests
{
    [Test]
    [Combinatorial]
    public void GivenRegisteredHandlers_WhenCallingRegistry_ReturnsCorrectRegistrations(
        [Values("type", "factory", "instance", "delegate")]
        string registrationMethod)
    {
        var services = new ServiceCollection();

        _ = registrationMethod switch
        {
            "type" => services.AddConquerorCommandHandler<TestCommandHandler>()
                              .AddConquerorCommandHandler<TestCommand2Handler>()
                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                              .AddConquerorCommandHandler<TestCommandWithoutResponse2Handler>(),
            "factory" => services.AddConquerorCommandHandler(_ => new TestCommandHandler())
                                 .AddConquerorCommandHandler(_ => new TestCommand2Handler())
                                 .AddConquerorCommandHandler(_ => new TestCommandWithoutResponseHandler())
                                 .AddConquerorCommandHandler(_ => new TestCommandWithoutResponse2Handler()),
            "instance" => services.AddConquerorCommandHandler(new TestCommandHandler())
                                  .AddConquerorCommandHandler(new TestCommand2Handler())
                                  .AddConquerorCommandHandler(new TestCommandWithoutResponseHandler())
                                  .AddConquerorCommandHandler(new TestCommandWithoutResponse2Handler()),
            "delegate" => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => throw new NotSupportedException())
                                  .AddConquerorCommandHandlerDelegate<TestCommand2, TestCommand2Response>((_, _, _) => throw new NotSupportedException())
                                  .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => throw new NotSupportedException())
                                  .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse2>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(registrationMethod), registrationMethod, null),
        };

        using var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestCommand), typeof(TestCommandResponse), new InProcessCommandAttribute()),
            (typeof(TestCommand2), typeof(TestCommand2Response), new()),
            (typeof(TestCommandWithoutResponse), null, new()),
            (typeof(TestCommandWithoutResponse2), null, new()),
        };

        var registrations = registry.GetCommandTypesForTransport<InProcessCommandAttribute>();

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
        Func<IServiceProvider, TestCommandHandler> factory = _ => new();
        var instance = new TestCommandHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorCommandHandler<TestCommandHandler>(),
                (null, "factory") => services.AddConquerorCommandHandler(factory),
                (var l, "type") => services.AddConquerorCommandHandler<TestCommandHandler>(l.Value),
                (var l, "factory") => services.AddConquerorCommandHandler(factory, l.Value),
                (_, "instance") => services.AddConquerorCommandHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResponse>)), Is.EqualTo(1));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandler)).ImplementationType, Is.EqualTo(typeof(TestCommandHandler)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandler)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandler)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());

        var registry = provider.GetRequiredService<ICommandTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestCommand), typeof(TestCommandResponse), new InProcessCommandAttribute()),
        };

        var registrations = registry.GetCommandTypesForTransport<InProcessCommandAttribute>();

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
        Func<IServiceProvider, TestCommandWithoutResponseHandler> factory = _ => new();
        var instance = new TestCommandWithoutResponseHandler();

        void Register(ServiceLifetime? lifetime, string method)
        {
            _ = (lifetime, method) switch
            {
                (null, "type") => services.AddConquerorCommandHandler<TestCommandWithoutResponseHandler>(),
                (null, "factory") => services.AddConquerorCommandHandler(factory),
                (var l, "type") => services.AddConquerorCommandHandler<TestCommandWithoutResponseHandler>(l.Value),
                (var l, "factory") => services.AddConquerorCommandHandler(factory, l.Value),
                (_, "instance") => services.AddConquerorCommandHandler(instance),
                _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
            };
        }

        Register(initialLifetime, initialRegistrationMethod);
        Register(overwrittenLifetime, overwrittenRegistrationMethod);

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponse>)), Is.EqualTo(1));

        switch (overwrittenLifetime, overwrittenRegistrationMethod)
        {
            case (var l, "type"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)).ImplementationType, Is.EqualTo(typeof(TestCommandWithoutResponseHandler)));
                break;
            case (var l, "factory"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)).Lifetime, Is.EqualTo(l ?? ServiceLifetime.Transient));
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)).ImplementationFactory, Is.SameAs(factory));
                break;
            case (_, "instance"):
                Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)).ImplementationInstance, Is.SameAs(instance));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null);
        }

        using var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());

        var registry = provider.GetRequiredService<ICommandTransportRegistry>();

        var expectedRegistrations = new[]
        {
            (typeof(TestCommandWithoutResponse), (Type?)null, new InProcessCommandAttribute()),
        };

        var registrations = registry.GetCommandTypesForTransport<InProcessCommandAttribute>();

        Assert.That(registrations, Is.EqualTo(expectedRegistrations));
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandler_WhenRegisteringDifferentHandlerForSameCommandType_ThrowsInvalidOperationException(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestCommandHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestCommandHandler> duplicateFactory = _ => new();
        var instance = new TestCommandHandler();
        var duplicateInstance = new DuplicateTestCommandHandler();

        _ = (initialLifetime, initialRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorCommandHandler<TestCommandHandler>(),
            (null, "factory") => services.AddConquerorCommandHandler(factory),
            (var l, "type") => services.AddConquerorCommandHandler<TestCommandHandler>(l.Value),
            (var l, "factory") => services.AddConquerorCommandHandler(factory, l.Value),
            (_, "instance") => services.AddConquerorCommandHandler(instance),
            (_, "delegate") => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null),
        };

        _ = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = (overwrittenLifetime, overwrittenRegistrationMethod) switch
            {
                (null, "type") => services.AddConquerorCommandHandler<DuplicateTestCommandHandler>(),
                (null, "factory") => services.AddConquerorCommandHandler(duplicateFactory),
                (var l, "type") => services.AddConquerorCommandHandler<DuplicateTestCommandHandler>(l.Value),
                (var l, "factory") => services.AddConquerorCommandHandler(duplicateFactory, l.Value),
                (_, "instance") => services.AddConquerorCommandHandler(duplicateInstance),
                (_, "delegate") => services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommand2Response>((_, _, _) => throw new NotSupportedException()),
                _ => throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null),
            };
        });
    }

    [Test]
    [Combinatorial]
    public void GivenRegisteredHandlerWithoutResponse_WhenRegisteringDifferentHandlerForSameCommandType_ThrowsInvalidOperationException(
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? initialLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string initialRegistrationMethod,
        [Values(null, ServiceLifetime.Transient, ServiceLifetime.Scoped, ServiceLifetime.Singleton)]
        ServiceLifetime? overwrittenLifetime,
        [Values("type", "factory", "instance", "delegate")]
        string overwrittenRegistrationMethod)
    {
        var services = new ServiceCollection();
        Func<IServiceProvider, TestCommandWithoutResponseHandler> factory = _ => new();
        Func<IServiceProvider, DuplicateTestCommandWithoutResponseHandler> duplicateFactory = _ => new();
        var instance = new TestCommandWithoutResponseHandler();
        var duplicateInstance = new DuplicateTestCommandWithoutResponseHandler();

        _ = (initialLifetime, initialRegistrationMethod) switch
        {
            (null, "type") => services.AddConquerorCommandHandler<TestCommandWithoutResponseHandler>(),
            (null, "factory") => services.AddConquerorCommandHandler(factory),
            (var l, "type") => services.AddConquerorCommandHandler<TestCommandWithoutResponseHandler>(l.Value),
            (var l, "factory") => services.AddConquerorCommandHandler(factory, l.Value),
            (_, "instance") => services.AddConquerorCommandHandler(instance),
            (_, "delegate") => services.AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => throw new NotSupportedException()),
            _ => throw new ArgumentOutOfRangeException(nameof(initialRegistrationMethod), initialRegistrationMethod, null),
        };

        _ = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = (overwrittenLifetime, overwrittenRegistrationMethod) switch
            {
                (null, "type") => services.AddConquerorCommandHandler<DuplicateTestCommandWithoutResponseHandler>(),
                (null, "factory") => services.AddConquerorCommandHandler(duplicateFactory),
                (var l, "type") => services.AddConquerorCommandHandler<DuplicateTestCommandWithoutResponseHandler>(l.Value),
                (var l, "factory") => services.AddConquerorCommandHandler(duplicateFactory, l.Value),
                (_, "instance") => services.AddConquerorCommandHandler(duplicateInstance),
                (_, "delegate") => services.AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => throw new NotSupportedException()),
                _ => throw new ArgumentOutOfRangeException(nameof(overwrittenRegistrationMethod), overwrittenRegistrationMethod, null),
            };
        });
    }

    [Test]
    public void GivenRegisteredHandlerTypes_WhenRegisteringHandlersViaAssemblyScanning_DoesNotOverwriteRegistrations()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerForAssemblyScanning>(ServiceLifetime.Singleton)
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandlerForAssemblyScanning>(ServiceLifetime.Singleton)
                                              .AddConquerorCQSTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandHandlerForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandForAssemblyScanning, TestCommandResponseForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandlerForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandWithoutResponseHandlerForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponseForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandlerForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<ICommandTransportRegistry>().GetCommandTypesForTransport<InProcessCommandAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestCommandForAssemblyScanning), typeof(TestCommandResponseForAssemblyScanning), new InProcessCommandAttribute())));
        Assert.That(registrations, Has.One.EqualTo((typeof(TestCommandWithoutResponseForAssemblyScanning), (Type?)null, new InProcessCommandAttribute())));
    }

    [Test]
    public void GivenServiceCollection_WhenRegisteringHandlersViaAssemblyScanningMultipleTimes_DoesNotOverwriteRegistrations()
    {
        var services = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly()
                                              .AddConquerorCQSTypesFromExecutingAssembly();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandHandlerForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandForAssemblyScanning, TestCommandResponseForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandHandlerForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Transient));

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandWithoutResponseHandlerForAssemblyScanning)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponseForAssemblyScanning>)), Is.EqualTo(1));
        Assert.That(services.Single(s => s.ServiceType == typeof(TestCommandWithoutResponseHandlerForAssemblyScanning)).Lifetime, Is.EqualTo(ServiceLifetime.Transient));

        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<ICommandTransportRegistry>().GetCommandTypesForTransport<InProcessCommandAttribute>();

        Assert.That(registrations, Has.One.EqualTo((typeof(TestCommandForAssemblyScanning), typeof(TestCommandResponseForAssemblyScanning), new InProcessCommandAttribute())));
        Assert.That(registrations, Has.One.EqualTo((typeof(TestCommandWithoutResponseForAssemblyScanning), (Type?)null, new InProcessCommandAttribute())));
    }

    private sealed record TestCommand;

    private sealed record TestCommandResponse;

    private sealed record TestCommand2;

    private sealed record TestCommand2Response;

    private sealed record TestCommandWithoutResponse;

    private sealed record TestCommandWithoutResponse2;

    public sealed record TestCommandForAssemblyScanning;

    public sealed record TestCommandWithoutResponseForAssemblyScanning;

    public sealed record TestCommandResponseForAssemblyScanning;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestCommand2Handler : ICommandHandler<TestCommand2, TestCommand2Response>
    {
        public Task<TestCommand2Response> Handle(TestCommand2 command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestCommandHandler : ICommandHandler<TestCommand, TestCommand2Response>
    {
        public Task<TestCommand2Response> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestCommandWithoutResponse2Handler : ICommandHandler<TestCommandWithoutResponse2>
    {
        public Task Handle(TestCommandWithoutResponse2 command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DuplicateTestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestCommandHandlerForAssemblyScanning : ICommandHandler<TestCommandForAssemblyScanning, TestCommandResponseForAssemblyScanning>
    {
        public Task<TestCommandResponseForAssemblyScanning> Handle(TestCommandForAssemblyScanning command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public sealed class TestCommandWithoutResponseHandlerForAssemblyScanning : ICommandHandler<TestCommandWithoutResponseForAssemblyScanning>
    {
        public Task Handle(TestCommandWithoutResponseForAssemblyScanning command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
