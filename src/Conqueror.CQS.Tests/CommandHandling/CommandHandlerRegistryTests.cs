using Conqueror.CQS.CommandHandling;

namespace Conqueror.CQS.Tests.CommandHandling;

[TestFixture]
public sealed class CommandHandlerRegistryTests
{
    [Test]
    public void GivenManuallyRegisteredCommandHandler_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithPipeline_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithPipeline>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.Multiple(() =>
        {
            Assert.That(registrations.Single().CommandType, Is.EqualTo(typeof(TestCommand)));
            Assert.That(registrations.Single().ResponseType, Is.EqualTo(typeof(TestCommandResponse)));
            Assert.That(registrations.Single().HandlerType, Is.EqualTo(typeof(TestCommandHandlerWithPipeline)));
            Assert.That(registrations.Single().ConfigurePipeline, Is.Not.Null);
        });
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandler_WhenRegisteringDifferentHandlerForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandler<TestCommandHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler2), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandler_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandlerWithCustomInterface), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandler_WhenRegisteringHandlerDelegateForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(DelegateCommandHandler<TestCommand, TestCommandResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterface_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandlerWithCustomInterface), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterface_WhenRegisteringDifferentHandlerForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface>()
                                              .AddConquerorCommandHandler<TestCommandHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler2), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterface_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface>()
                                              .AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandlerWithCustomInterface2), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterface_WhenRegisteringHandlerDelegateForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface>()
                                              .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(DelegateCommandHandler<TestCommand, TestCommandResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegate_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(DelegateCommandHandler<TestCommand, TestCommandResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegate_WhenRegisteringDifferentHandlerForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .AddConquerorCommandHandler<TestCommandHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegate_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .AddConquerorCommandHandler<TestCommandHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandlerWithCustomInterface), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegate_WhenRegisteringHandlerDelegateForSameCommandAndResponseType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>((_, _, _) => Task.FromResult(new TestCommandResponse()))
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(DelegateCommandHandler<TestCommand, TestCommandResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithoutResponse_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandler), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithoutResponse_WhenRegisteringDifferentHandlerForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandler2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandler2), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithoutResponse_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandlerWithCustomInterface), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithoutResponse_WhenRegisteringHandlerDelegateForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(DelegateCommandHandler<TestCommandWithoutResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterfaceWithoutResponse_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandlerWithCustomInterface), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterfaceWithoutResponse_WhenRegisteringDifferentHandlerForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandler), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterfaceWithoutResponse_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface2>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandlerWithCustomInterface2), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerWithCustomInterfaceWithoutResponse_WhenRegisteringHandlerDelegateForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                              .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(DelegateCommandHandler<TestCommandWithoutResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegateWithoutResponse_ReturnsRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(DelegateCommandHandler<TestCommandWithoutResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegateWithoutResponse_WhenRegisteringDifferentHandlerForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandler), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegateWithoutResponse_WhenRegisteringDifferentHandlerWithCustomInterfaceForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandlerWithCustomInterface>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(TestCommandWithoutResponseHandlerWithCustomInterface), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenManuallyRegisteredCommandHandlerDelegateWithoutResponse_WhenRegisteringHandlerDelegateForSameCommandType_ReturnsOverwrittenRegistration()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>((_, _, _) => Task.CompletedTask)
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommandWithoutResponse), null, typeof(DelegateCommandHandler<TestCommandWithoutResponse>), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenMultipleManuallyRegisteredCommandHandlers_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandler<TestCommand2Handler>()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var expectedRegistrations = new[]
        {
            new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler), null),
            new CommandHandlerRegistration(typeof(TestCommand2), typeof(TestCommand2Response), typeof(TestCommand2Handler), null),
        };

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
    }

    [Test]
    public void GivenCommandHandlersRegisteredViaAssemblyScanning_ReturnsRegistrations()
    {
        var provider = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly()
                                              .BuildServiceProvider();

        var registry = provider.GetRequiredService<ICommandHandlerRegistry>();

        var registrations = registry.GetCommandHandlerRegistrations();

        Assert.That(registrations, Contains.Item(new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandler), null))
                                           .Or.Contains(new CommandHandlerRegistration(typeof(TestCommand), typeof(TestCommandResponse), typeof(TestCommandHandlerWithCustomInterface), null)));
        Assert.That(registrations, Contains.Item(new CommandHandlerRegistration(typeof(TestCommand2), typeof(TestCommand2Response), typeof(TestCommand2Handler), null)));
    }

    [Test]
    public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCommandHandler<TestCommandHandlerWithoutValidInterfaces>(_ => new()));
    }

    [Test]
    public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandHandler<TestCommandHandlerWithCustomInterfaceWithExtraMethod>());
    }

    [Test]
    public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleInterfaces>();

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand2, TestCommand2Response>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler2>());
    }

    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    public sealed record TestCommand2;

    public sealed record TestCommand2Response;

    public sealed record TestCommandWithoutResponse;

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandler2 : ICommandHandler<TestCommand2, TestCommand2Response>;

    public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
    {
        void ExtraMethod();
    }

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>;

    public sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    private sealed class TestCommandHandler2 : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    public sealed class TestCommandHandlerWithCustomInterface : ITestCommandHandler
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    private sealed class TestCommandHandlerWithCustomInterface2 : ITestCommandHandler
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());
    }

    public sealed class TestCommand2Handler : ICommandHandler<TestCommand2, TestCommand2Response>
    {
        public Task<TestCommand2Response> Handle(TestCommand2 command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommand2Response());
    }

    public sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestCommandWithoutResponseHandler2 : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestCommandWithoutResponseHandlerWithCustomInterface : ITestCommandWithoutResponseHandler
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestCommandWithoutResponseHandlerWithCustomInterface2 : ITestCommandWithoutResponseHandler
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestCommandHandlerWithCustomInterfaceWithExtraMethod : ITestCommandHandlerWithExtraMethod
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestCommandHandlerWithMultipleInterfaces : ITestCommandHandler,
                                                                    ITestCommandHandler2
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TestCommand2Response> Handle(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestCommandHandlerWithPipeline : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => Task.FromResult(new TestCommandResponse());

        public static void ConfigurePipeline(ICommandPipeline<TestCommand, TestCommandResponse> pipeline)
        {
        }
    }

    private sealed class TestCommandHandlerWithoutValidInterfaces : ICommandHandler;
}
