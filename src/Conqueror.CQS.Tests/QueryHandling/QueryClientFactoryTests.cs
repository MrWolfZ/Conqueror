namespace Conqueror.CQS.Tests.QueryHandling;

public abstract class QueryClientFactoryTests
{
    [Test]
    public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<ITestQueryHandlerWithExtraMethod>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenNonGenericQueryHandlerInterface_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<INonGenericQueryHandler>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenConcreteQueryHandlerType_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<TestQueryHandler>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenQueryHandlerInterfaceThatImplementsMultipleOtherPlainQueryHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<ICombinedQueryHandler>(clientFactory, b => b.UseInProcess()));
    }

    [Test]
    public void GivenQueryHandlerInterfaceThatImplementsMultipleOtherCustomQueryHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var provider = new ServiceCollection().AddConquerorCQS().BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<ICombinedCustomQueryHandler>(clientFactory, b => b.UseInProcess()));
    }

    protected abstract THandler CreateQueryClient<THandler>(IQueryClientFactory clientFactory,
                                                            Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
        where THandler : class, IQueryHandler;

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public sealed record TestQuery2;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    public interface ITestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse>;

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }

    public interface ICombinedQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IQueryHandler<TestQuery2, TestQueryResponse>;

    public interface ICombinedCustomQueryHandler : ITestQueryHandler, ITestQueryHandler2;

    public interface INonGenericQueryHandler : IQueryHandler
    {
        void SomeMethod();
    }

    private sealed class TestQueryHandler : ITestQueryHandler
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

[TestFixture]
public sealed class QueryClientFactoryWithSyncFactoryTests : QueryClientFactoryTests
{
    protected override THandler CreateQueryClient<THandler>(IQueryClientFactory clientFactory,
                                                            Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        return clientFactory.CreateQueryClient<THandler>(transportClientFactory);
    }
}

[TestFixture]
public sealed class QueryClientFactoryWithAsyncFactoryTests : QueryClientFactoryTests
{
    protected override THandler CreateQueryClient<THandler>(IQueryClientFactory clientFactory,
                                                            Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        return clientFactory.CreateQueryClient<THandler>(async b =>
                                                         {
                                                             await Task.Delay(1);
                                                             return transportClientFactory(b);
                                                         });
    }
}
