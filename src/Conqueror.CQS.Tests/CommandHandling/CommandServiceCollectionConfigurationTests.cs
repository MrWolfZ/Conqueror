namespace Conqueror.CQS.Tests.CommandHandling;

[TestFixture]
public sealed class CommandServiceCollectionConfigurationTests
{
    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

        Assert.DoesNotThrow(() => services.AddConquerorCommandHandler<TestCommandHandler>());
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingIdenticalHandlerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandler<TestCommandHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerTypeWithoutResponse_AddingIdenticalHandlerDoesNotThrow()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>();

        Assert.DoesNotThrow(() => services.AddConquerorCommandHandler<TestCommandWithoutResponseHandler>());
    }

    [Test]
    public void GivenRegisteredHandlerTypeWithoutResponse_AddingIdenticalHandlerOnlyKeepsOneRegistration()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .AddConquerorCommandHandler<TestCommandWithoutResponseHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameCommandAndResponseTypesKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>()
                                              .AddConquerorCommandHandler<DuplicateTestCommandHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestCommandHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommand, TestCommandResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithSameCommandTypeAndDifferentResponseTypeThrowsInvalidOperationException()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandHandler>();

        _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorCommandHandler<DuplicateTestCommandHandlerWithDifferentResponseType>());
    }

    [Test]
    public void GivenRegisteredHandlerType_AddingHandlerWithoutResponseWithSameCommandTypeKeepsBothRegistrations()
    {
        var services = new ServiceCollection().AddConquerorCommandHandler<TestCommandWithoutResponseHandler>()
                                              .AddConquerorCommandHandler<DuplicateTestCommandWithoutResponseHandler>();

        Assert.That(services.Count(s => s.ServiceType == typeof(TestCommandWithoutResponseHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(DuplicateTestCommandWithoutResponseHandler)), Is.EqualTo(1));
        Assert.That(services.Count(s => s.ServiceType == typeof(ICommandHandler<TestCommandWithoutResponse>)), Is.EqualTo(1));
    }

    [Test]
    public void GivenHandlerTypeWithInstanceFactory_AddedHandlerCanBeResolvedFromInterface()
    {
        var provider = new ServiceCollection().AddConquerorCommandHandler(_ => new TestCommandHandler())
                                              .BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    private sealed record TestCommand;

    private sealed record TestCommandWithoutResponse;

    private sealed record TestCommandResponse;

    private sealed record TestCommandResponse2;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DuplicateTestCommandHandlerWithDifferentResponseType : ICommandHandler<TestCommand, TestCommandResponse2>
    {
        public Task<TestCommandResponse2> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DuplicateTestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
        public Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
