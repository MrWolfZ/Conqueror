namespace Conqueror.CQS.Tests.CommandHandling;

public abstract class CommandClientFactoryTests
{
    [Test]
    public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<ITestCommandHandlerWithExtraMethod>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenNonGenericCommandHandlerInterface_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<INonGenericCommandHandler>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenConcreteCommandHandlerType_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<TestCommandHandler>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenCommandHandlerInterfaceThatImplementsMultipleOtherPlainCommandHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<ICombinedCommandHandler>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenCommandHandlerInterfaceThatImplementsMultipleOtherCustomCommandHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateCommandClient<ICombinedCustomCommandHandler>(clientFactory, b => b.UseInProcess()));
    }

    protected abstract THandler CreateCommandClient<THandler>(ICommandClientFactory clientFactory,
                                                              Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
        where THandler : class, ICommandHandler;

    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    public sealed record TestCommandWithoutResponse;

    public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>;

    public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>;

    public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
    {
        void ExtraMethod();
    }

    public interface ICombinedCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, ICommandHandler<TestCommandWithoutResponse>;

    public interface ICombinedCustomCommandHandler : ITestCommandHandler, ITestCommandWithoutResponseHandler;

    public interface INonGenericCommandHandler : ICommandHandler
    {
        void SomeMethod();
    }

    private sealed class TestCommandHandler : ITestCommandHandler
    {
        public Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

[TestFixture]
public sealed class CommandClientFactoryWithSyncFactoryTests : CommandClientFactoryTests
{
    protected override THandler CreateCommandClient<THandler>(ICommandClientFactory clientFactory,
                                                              Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory)
    {
        return clientFactory.CreateCommandClient<THandler>(transportClientFactory);
    }
}

[TestFixture]
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
