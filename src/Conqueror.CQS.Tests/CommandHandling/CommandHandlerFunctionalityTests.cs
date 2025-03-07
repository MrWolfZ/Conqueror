namespace Conqueror.CQS.Tests.CommandHandling;

public abstract class CommandHandlerFunctionalityTests
{
    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected abstract IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services);

    protected virtual ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
    }

    protected virtual ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
    }

    protected virtual TestCommand CreateCommand() => new(10);

    protected virtual TestCommandWithoutResponse CreateCommandWithoutResponse() => new(20);

    protected virtual TestCommandResponse CreateExpectedResponse() => new(11);

    [Test]
    public async Task GivenCommand_HandlerReceivesCommand()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var command = CreateCommand();

        _ = await handler.Handle(command);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenCommandWithoutResponse_HandlerReceivesCommand()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        var command = CreateCommandWithoutResponse();

        await handler.Handle(command);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(CreateCommand(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationTokenForHandlerWithoutResponse_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);
        using var tokenSource = new CancellationTokenSource();

        await handler.Handle(CreateCommandWithoutResponse(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        _ = await handler.Handle(CreateCommand());

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerWithoutResponseReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        await handler.Handle(CreateCommandWithoutResponse());

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenCommand_HandlerReturnsResponse()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var command = CreateCommand();

        var response = await handler.Handle(command);

        Assert.That(response.Payload, Is.EqualTo(command.Payload + 1));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var observations = new TestObservations();
        var exception = new Exception();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .AddSingleton(exception)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(CreateCommand()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenExceptionInHandlerWithoutResponse_InvocationThrowsSameException()
    {
        var observations = new TestObservations();
        var exception = new Exception();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .AddSingleton(exception)
                       .BuildServiceProvider();

        var handler = ResolveHandlerWithoutResponse(provider);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(CreateCommandWithoutResponse()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandler_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await handler1.Handle(CreateCommand());
        _ = await handler1.Handle(CreateCommand());
        _ = await handler2.Handle(CreateCommand());

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1]));
        Assert.That(observations.ServiceProviders[0], Is.Not.SameAs(observations.ServiceProviders[2]));
    }

    [Test]
    public async Task GivenHandlerWithoutResponse_HandlerIsResolvedFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandlerWithoutResponse(scope1.ServiceProvider);
        var handler2 = ResolveHandlerWithoutResponse(scope2.ServiceProvider);

        await handler1.Handle(CreateCommandWithoutResponse());
        await handler1.Handle(CreateCommandWithoutResponse());
        await handler2.Handle(CreateCommandWithoutResponse());

        Assert.That(observations.ServiceProviders, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProviders[0], Is.SameAs(observations.ServiceProviders[1]));
        Assert.That(observations.ServiceProviders[0], Is.Not.SameAs(observations.ServiceProviders[2]));
    }

    public record TestCommand(int Payload);

    public record TestCommandResponse(int Payload);

    public record TestCommandWithoutResponse(int Payload);

    protected sealed class TestObservations
    {
        public List<object> Commands { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<IServiceProvider> ServiceProviders { get; } = [];

        public List<IServiceProvider> ServiceProvidersFromTransportFactory { get; } = [];
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityDefaultTests : CommandHandlerFunctionalityTests
{
    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services.AddConquerorCommandHandler<DisposableCommandHandler>()
                    .AddSingleton(observation);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

        _ = await handler.Handle(new(10));

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorCommandHandler<TestCommandHandler>();
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>();
    }

    private sealed class TestCommandHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null) : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
            return new(command.Payload + 1);
        }
    }

    private sealed class TestCommandHandlerWithoutResponse(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null) : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class DisposableCommandHandler(DisposalObservation observation) : ICommandHandler<TestCommand, TestCommandResponse>, IDisposable
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(command.Payload);
        }

        public void Dispose() => observation.WasDisposed = true;
    }

    private sealed class DisposalObservation
    {
        public bool WasDisposed { get; set; }
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityDelegateTests : CommandHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(async (command, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Commands.Add(command);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);
            return new(command.Payload + 1);
        });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>(async (command, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Commands.Add(command);
            obs.CancellationTokens.Add(cancellationToken);
            obs.ServiceProviders.Add(p);
        });
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityGenericTests : CommandHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorCommandHandler<GenericTestCommandHandler<string>>();
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddConquerorCommandHandler<GenericTestCommandHandlerWithoutResponse<string>>();
    }

    protected override ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<GenericTestCommand<string>, GenericTestCommandResponse<string>>>();
        return new AdapterHandler<string>(handler);
    }

    protected override ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<GenericTestCommandWithoutResponse<string>>>();
        return new AdapterHandlerWithoutResponse<string>(handler);
    }

    protected override TestCommand CreateCommand() => new GenericTestCommand<string>("test");

    protected override TestCommandWithoutResponse CreateCommandWithoutResponse() => new GenericTestCommandWithoutResponse<string>("test");

    protected override TestCommandResponse CreateExpectedResponse() => new GenericTestCommandResponse<string>("test");

    private sealed record GenericTestCommand<T>(T GenericPayload) : TestCommand(10);

    private sealed record GenericTestCommandWithoutResponse<T>(T GenericPayload) : TestCommandWithoutResponse(10);

    private sealed record GenericTestCommandResponse<T>(T GenericPayload) : TestCommandResponse(11);

    private sealed class GenericTestCommandHandler<T>(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null) : ICommandHandler<GenericTestCommand<T>, GenericTestCommandResponse<T>>
    {
        public async Task<GenericTestCommandResponse<T>> Handle(GenericTestCommand<T> command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
            return new(command.GenericPayload);
        }
    }

    private sealed class GenericTestCommandHandlerWithoutResponse<T>(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null) : ICommandHandler<GenericTestCommandWithoutResponse<T>>
    {
        public async Task Handle(GenericTestCommandWithoutResponse<T> command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }

    private sealed class AdapterHandler<T>(ICommandHandler<GenericTestCommand<T>, GenericTestCommandResponse<T>> wrapped) : ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return await wrapped.Handle((GenericTestCommand<T>)command, cancellationToken);
        }
    }

    private sealed class AdapterHandlerWithoutResponse<T>(ICommandHandler<GenericTestCommandWithoutResponse<T>> wrapped) : ICommandHandler<TestCommandWithoutResponse>
    {
        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            await wrapped.Handle((GenericTestCommandWithoutResponse<T>)command, cancellationToken);
        }
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityCustomInterfaceTests : CommandHandlerFunctionalityTests
{
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

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorCommandHandler<TestCommandHandler>();
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddConquerorCommandHandler<TestCommandHandlerWithoutResponse>();
    }

    protected override ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ITestCommandHandler>();
    }

    protected override ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>;

    private sealed class TestCommandHandler(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null) : ITestCommandHandler
    {
        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
            return new(command.Payload + 1);
        }
    }

    private sealed class TestCommandHandlerWithoutResponse(TestObservations observations, IServiceProvider serviceProvider, Exception? exception = null) : ITestCommandHandlerWithoutResponse
    {
        public async Task Handle(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);
        }
    }
}

public abstract class CommandHandlerFunctionalityClientTests : CommandHandlerFunctionalityTests
{
    [Test]
    public async Task GivenHandlerClient_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandler(scope1.ServiceProvider);
        var handler2 = ResolveHandler(scope2.ServiceProvider);

        _ = await handler1.Handle(CreateCommand());
        _ = await handler1.Handle(CreateCommand());
        _ = await handler2.Handle(CreateCommand());

        Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.SameAs(observations.ServiceProvidersFromTransportFactory[1]));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2]));
    }

    [Test]
    public async Task GivenHandlerClientWithoutResponse_ServiceProviderInTransportBuilderIsFromResolutionScope()
    {
        var observations = new TestObservations();

        var provider = RegisterHandlerWithoutResponse(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = ResolveHandlerWithoutResponse(scope1.ServiceProvider);
        var handler2 = ResolveHandlerWithoutResponse(scope2.ServiceProvider);

        await handler1.Handle(CreateCommandWithoutResponse());
        await handler1.Handle(CreateCommandWithoutResponse());
        await handler2.Handle(CreateCommandWithoutResponse());

        Assert.That(observations.ServiceProvidersFromTransportFactory, Has.Count.EqualTo(3));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.SameAs(observations.ServiceProvidersFromTransportFactory[1]));
        Assert.That(observations.ServiceProvidersFromTransportFactory[0], Is.Not.SameAs(observations.ServiceProvidersFromTransportFactory[2]));
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddSingleton<TestCommandTransport>();
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return services.AddSingleton<TestCommandTransport>();
    }

    protected sealed class TestCommandTransport(Exception? exception = null) : ICommandTransportClient
    {
        public string TransportTypeName => "test";

        public async Task<TResponse> Send<TCommand, TResponse>(TCommand command,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TCommand : class
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            var observations = serviceProvider.GetRequiredService<TestObservations>();
            observations.Commands.Add(command);
            observations.CancellationTokens.Add(cancellationToken);
            observations.ServiceProviders.Add(serviceProvider);

            if (command is TestCommandWithoutResponse)
            {
                return (TResponse)(object)UnitCommandResponse.Instance;
            }

            var cmd = (TestCommand)(object)command;
            return (TResponse)(object)new TestCommandResponse(cmd.Payload + 1);
        }
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientWithSyncTransportFactoryTests : CommandHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b =>
                   {
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b =>
                   {
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientCustomInterfaceWithSyncTransportFactoryTests : CommandHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ITestCommandHandler>(b =>
                   {
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ITestCommandHandlerWithoutResponse>(b =>
                   {
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>;
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientWithAsyncTransportFactoryTests : CommandHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async b =>
                   {
                       await Task.Delay(1);
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(async b =>
                   {
                       await Task.Delay(1);
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientCustomInterfaceWithAsyncTransportFactoryTests : CommandHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ITestCommandHandler>(async b =>
                   {
                       await Task.Delay(1);
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorCommandClient<ITestCommandHandlerWithoutResponse>(async b =>
                   {
                       await Task.Delay(1);
                       b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                       return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                   });
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>;
}

public abstract class CommandHandlerFunctionalityClientFromFactoryTests : CommandHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services).AddConquerorCommandHandlerDelegate<TestCommand, TestCommandResponse>(async (command, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Commands.Add(command);
            obs.CancellationTokens.Add(cancellationToken);
            return new(command.Payload + 1);
        });
    }

    protected override IServiceCollection RegisterHandlerWithoutResponse(IServiceCollection services)
    {
        return base.RegisterHandler(services).AddConquerorCommandHandlerDelegate<TestCommandWithoutResponse>(async (command, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Commands.Add(command);
            obs.CancellationTokens.Add(cancellationToken);
        });
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientFromFactoryWithSyncTransportFactoryTests : CommandHandlerFunctionalityClientFromFactoryTests
{
    protected override ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b =>
                              {
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }

    protected override ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b =>
                              {
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientWithCustomInterfaceFromFactoryWithSyncTransportFactoryTests : CommandHandlerFunctionalityClientFromFactoryTests
{
    protected override ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ITestCommandHandler>(b =>
                              {
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }

    protected override ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ITestCommandHandlerWithoutResponse>(b =>
                              {
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>;
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientFromFactoryWithAsyncTransportFactoryTests : CommandHandlerFunctionalityClientFromFactoryTests
{
    protected override ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(async b =>
                              {
                                  await Task.Delay(1);
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }

    protected override ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ICommandHandler<TestCommandWithoutResponse>>(async b =>
                              {
                                  await Task.Delay(1);
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }
}

[TestFixture]
public sealed class CommandHandlerFunctionalityClientWithCustomInterfaceFromFactoryWithAsyncTransportFactoryTests : CommandHandlerFunctionalityClientFromFactoryTests
{
    protected override ICommandHandler<TestCommand, TestCommandResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ITestCommandHandler>(async b =>
                              {
                                  await Task.Delay(1);
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }

    protected override ICommandHandler<TestCommandWithoutResponse> ResolveHandlerWithoutResponse(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ICommandClientFactory>()
                              .CreateCommandClient<ITestCommandHandlerWithoutResponse>(async b =>
                              {
                                  await Task.Delay(1);
                                  b.ServiceProvider.GetRequiredService<TestObservations>().ServiceProvidersFromTransportFactory.Add(b.ServiceProvider);
                                  return b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                              });
    }

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>;
}
