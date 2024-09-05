namespace Conqueror.CQS.Tests.CommandHandling;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public abstract class CommandClientFactoryTests
{
    [Test]
    public async Task GivenPlainHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        var client = CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

        var command = new TestCommand();

        _ = await client.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        var client = CreateCommandClient<ITestCommandHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

        var command = new TestCommand();

        _ = await client.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenPlainHandlerInterfaceWithoutResponse_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        var client = CreateCommandClient<ICommandHandler<TestCommandWithoutResponse>>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

        var command = new TestCommandWithoutResponse();

        await client.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenCustomHandlerInterfaceWithoutResponse_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        var client = CreateCommandClient<ITestCommandWithoutResponseHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

        var command = new TestCommandWithoutResponse();

        await client.ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
    }

    [Test]
    public async Task GivenPlainClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddTransient<TestCommandTransport>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        var client = CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(clientFactory,
                                                                                            b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

        var command = new TestCommand();

        _ = await client.WithPipeline(p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()))
                        .ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
    }

    [Test]
    public async Task GivenCustomClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddTransient<TestCommandTransport>()
                    .AddConquerorCommandMiddleware<TestCommandMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        var client = CreateCommandClient<ITestCommandHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

        var command = new TestCommand();

        _ = await client.WithPipeline(p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()))
                        .ExecuteCommand(command, CancellationToken.None);

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
    }

    [Test]
    public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<ITestCommandHandlerWithExtraMethod>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
    }

    [Test]
    public void GivenNonGenericCommandHandlerInterface_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<INonGenericCommandHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
    }

    [Test]
    public void GivenConcreteCommandHandlerType_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<TestCommandHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
    }

    [Test]
    public void GivenCommandHandlerInterfaceThatImplementsMultipleOtherPlainCommandHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<ICombinedCommandHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
    }

    [Test]
    public void GivenCommandHandlerInterfaceThatImplementsMultipleOtherCustomCommandHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestCommandTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<ICombinedCustomCommandHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
    }

    protected abstract THandler CreateCommandClient<THandler>(ICommandClientFactory clientFactory,
                                                              Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
        where THandler : class, ICommandHandler;

    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    public sealed record TestCommandWithoutResponse;

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
    {
    }

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
    {
    }

    public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
    {
        void ExtraMethod();
    }

    public interface ICombinedCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, ICommandHandler<TestCommandWithoutResponse>
    {
    }

    public interface ICombinedCustomCommandHandler : ITestCommandHandler, ITestCommandWithoutResponseHandler
    {
    }

    public interface INonGenericCommandHandler : ICommandHandler
    {
        void SomeMethod();
    }

    private sealed class TestCommandHandler : ITestCommandHandler
    {
        public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed record TestCommandMiddlewareConfiguration;

    private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestCommandMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
            where TCommand : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());

            return await ctx.Next(ctx.Command, ctx.CancellationToken);
        }
    }

    private sealed class TestCommandTransport : ICommandTransportClient
    {
        private readonly TestObservations responses;

        public TestCommandTransport(TestObservations responses)
        {
            this.responses = responses;
        }

        public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command,
                                                                         IServiceProvider serviceProvider,
                                                                         CancellationToken cancellationToken)
            where TCommand : class
        {
            await Task.Yield();
            responses.Commands.Add(command);

            if (typeof(TResponse) == typeof(UnitCommandResponse))
            {
                return (TResponse)(object)UnitCommandResponse.Instance;
            }

            return (TResponse)(object)new TestCommandResponse();
        }
    }

    private sealed class TestObservations
    {
        public List<object> Commands { get; } = new();

        public List<Type> MiddlewareTypes { get; } = new();
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class CommandClientFactoryWithSyncFactoryTests : CommandClientFactoryTests
{
    protected override THandler CreateCommandClient<THandler>(ICommandClientFactory clientFactory,
                                                              Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
    {
        return clientFactory.CreateCommandClient<THandler>(transportClientFactory);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class CommandClientFactoryWithAsyncFactoryTests : CommandClientFactoryTests
{
    protected override THandler CreateCommandClient<THandler>(ICommandClientFactory clientFactory,
                                                              Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
    {
        return clientFactory.CreateCommandClient<THandler>(async b =>
                                                           {
                                                               await Task.Delay(1);
                                                               return transportClientFactory(b);
                                                           });
    }
}
