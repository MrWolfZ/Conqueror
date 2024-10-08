namespace Conqueror.CQS.Tests.CommandHandling;

public sealed class CommandHandlerCustomInterfaceTests
{
    [Test]
    public async Task GivenCommand_HandlerReceivesCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestCommandHandler>();

        var command = new TestCommand(10);

        _ = await handler.Handle(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenGenericCommand_HandlerReceivesCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<GenericTestCommandHandler<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IGenericTestCommandHandler<string>>();

        var command = new GenericTestCommand<string>("test string");

        _ = await handler.Handle(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenCommandWithoutResponse_HandlerReceivesCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

        var command = new TestCommandWithoutResponse(10);

        await handler.Handle(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenGenericCommandWithoutResponse_HandlerReceivesCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<GenericTestCommandHandlerWithoutResponse<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IGenericTestCommandHandlerWithoutResponse<string>>();

        var command = new GenericTestCommand<string>("test string");

        await handler.Handle(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestCommandHandler>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenForHandlerWithoutResponse_HandlerReceivesCommand()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
        using var tokenSource = new CancellationTokenSource();

        await handler.Handle(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCommand_HandlerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestCommandHandler>();

        var command = new TestCommand(10);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.That(response.Payload, Is.EqualTo(command.Payload + 1));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorCommandHandler<ThrowingCommandHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IThrowingTestCommandHandler>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10), CancellationToken.None));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenHandlerWithoutResponseWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
    }

    [Test]
    public void GivenHandlerWithoutResponseWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandlerWithoutResponse>());
    }

    [Test]
    public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCommandHandler<TestCommandHandlerWithMultipleInterfaces>()
                    .AddConquerorQueryHandler<TestCommandHandlerWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler2>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandlerWithoutResponse>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCommandHandler<TestCommandHandlerWithCustomInterfaceWithExtraMethod>());
    }

    public sealed record TestCommand(int Payload = 0);

    public sealed record TestCommandResponse(int Payload);

    public sealed record TestCommand2;

    public sealed record TestCommandResponse2;

    public sealed record TestCommandWithoutResponse(int Payload = 0);

    public sealed record GenericTestCommand<T>(T Payload);

    public sealed record GenericTestCommandResponse<T>(T Payload);

    private sealed record TestQuery;

    private sealed record TestQueryResponse;

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>;

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>;

    public interface IGenericTestCommandHandler<T> : ICommandHandler<GenericTestCommand<T>, GenericTestCommandResponse<T>>;

    public interface IGenericTestCommandHandlerWithoutResponse<T> : ICommandHandler<GenericTestCommand<T>>;

    public interface IThrowingTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
    {
        void ExtraMethod();
    }

    private sealed class TestCommandHandler(TestObservations observations) : ITestCommandHandler
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            return new(command.Payload + 1);
        }
    }

    private sealed class TestCommandHandlerWithoutResponse(TestObservations observations) : ITestCommandHandlerWithoutResponse
    {
        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class TestCommandHandlerWithMultipleInterfaces(TestObservations observations) : ITestCommandHandler,
                                                                                                   ITestCommandHandler2,
                                                                                                   ITestCommandHandlerWithoutResponse,
                                                                                                   IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            return new(command.Payload + 1);
        }

        public async Task<TestCommandResponse2> Handle(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            return new();
        }

        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
        }

        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            return new();
        }
    }

    private sealed class GenericTestCommandHandler<T>(TestObservations responses) : IGenericTestCommandHandler<T>
    {
        public async Task<GenericTestCommandResponse<T>> Handle(GenericTestCommand<T> command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            responses.Commands.Add(command);
            responses.CancellationTokens.Add(cancellationToken);
            return new(command.Payload);
        }
    }

    private sealed class GenericTestCommandHandlerWithoutResponse<T>(TestObservations responses) : IGenericTestCommandHandlerWithoutResponse<T>
    {
        public async Task Handle(GenericTestCommand<T> command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            responses.Commands.Add(command);
            responses.CancellationTokens.Add(cancellationToken);
        }
    }

    private sealed class ThrowingCommandHandler(Exception exception) : IThrowingTestCommandHandler
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestCommandHandlerWithCustomInterfaceWithExtraMethod : ITestCommandHandlerWithExtraMethod
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = [];

        public List<object> Commands { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
