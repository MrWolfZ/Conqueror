namespace Conqueror.CQS.Tests.QueryHandling;

public abstract class QueryClientCustomInterfaceTests
{
    [Test]
    public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddQueryClient<ITestQueryHandler>(services, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        _ = services.AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<ITestQueryHandler>();

        var query = new TestQuery();

        _ = await client.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    protected abstract void AddQueryClient<THandler>(IServiceCollection services,
                                                     Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
        where THandler : class, IQueryHandler;

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    private sealed class TestQueryTransport(TestObservations responses) : IQueryTransportClient
    {
        public QueryTransportType TransportType { get; } = new("test", QueryTransportRole.Client);

        public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                                     IServiceProvider serviceProvider,
                                                                     CancellationToken cancellationToken)
            where TQuery : class
        {
            await Task.Yield();
            responses.Queries.Add(query);

            return (TResponse)(object)new TestQueryResponse();
        }
    }

    private sealed class TestObservations
    {
        public List<object> Queries { get; } = [];
    }
}

[TestFixture]
public sealed class QueryClientCustomInterfaceWithSyncFactoryTests : QueryClientCustomInterfaceTests
{
    protected override void AddQueryClient<THandler>(IServiceCollection services,
                                                     Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        _ = services.AddConquerorQueryClient<THandler>(transportClientFactory);
    }
}

[TestFixture]
public sealed class QueryClientCustomInterfaceWithAsyncFactoryTests : QueryClientCustomInterfaceTests
{
    protected override void AddQueryClient<THandler>(IServiceCollection services,
                                                     Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        _ = services.AddConquerorQueryClient<THandler>(async b =>
                                                       {
                                                           await Task.Delay(1);
                                                           return transportClientFactory(b);
                                                       });
    }
}
