namespace Conqueror.CQS.Tests.QueryHandling;

public abstract class QueryClientFunctionalityTests
{
    [Test]
    public async Task GivenQuery_TransportReceivesQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(services, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        _ = services.AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await client.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenCancellationToken_TransportReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(services, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        _ = services.AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        using var tokenSource = new CancellationTokenSource();

        _ = await client.ExecuteQuery(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenQuery_TransportReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(services, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        _ = services.AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        var response = await client.ExecuteQuery(query, CancellationToken.None);

        Assert.That(response.Payload, Is.EqualTo(query.Payload + 1));
    }

    [Test]
    public async Task GivenScopedFactory_TransportIsResolvedOnSameScope()
    {
        var seenInstances = new List<TestQueryTransport>();

        var services = new ServiceCollection();
        var observations = new TestObservations();

        AddQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(services, b =>
        {
            var transport = b.ServiceProvider.GetRequiredService<TestQueryTransport>();
            seenInstances.Add(transport);
            return transport;
        });

        _ = services.AddScoped<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var client1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var client2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var client3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await client1.ExecuteQuery(new(10), CancellationToken.None);
        _ = await client2.ExecuteQuery(new(10), CancellationToken.None);
        _ = await client3.ExecuteQuery(new(10), CancellationToken.None);

        Assert.That(seenInstances, Has.Count.EqualTo(3));
        Assert.That(seenInstances[1], Is.SameAs(seenInstances[0]));
        Assert.That(seenInstances[2], Is.Not.SameAs(seenInstances[0]));
    }

    [Test]
    public void GivenExceptionInTransport_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        AddQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(services, b => b.ServiceProvider.GetRequiredService<ThrowingTestQueryTransport>());

        _ = services.AddTransient<ThrowingTestQueryTransport>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenHandlerWithInvalidInterface_RegisteringClientThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => AddQueryClient<ITestQueryHandlerWithoutValidInterfaces>(new ServiceCollection(), _ => new ThrowingTestQueryTransport(new())));
    }

    protected abstract void AddQueryClient<THandler>(IServiceCollection services,
                                                     Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
        where THandler : class, IQueryHandler;

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int Payload);

    private interface ITestQueryHandlerWithoutValidInterfaces : IQueryHandler;

    private sealed class TestQueryTransport(TestObservations observations) : IQueryTransportClient
    {
        public string TransportTypeName => "test";

        public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                                     IServiceProvider serviceProvider,
                                                                     CancellationToken cancellationToken)
            where TQuery : class
        {
            await Task.Yield();
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);

            var cmd = (TestQuery)(object)query;
            return (TResponse)(object)new TestQueryResponse(cmd.Payload + 1);
        }
    }

    private sealed class ThrowingTestQueryTransport(Exception exception) : IQueryTransportClient
    {
        public string TransportTypeName => "test";

        public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query,
                                                                     IServiceProvider serviceProvider,
                                                                     CancellationToken cancellationToken)
            where TQuery : class
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestObservations
    {
        public List<object> Queries { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}

[TestFixture]
public sealed class QueryClientFunctionalityWithSyncFactoryTests : QueryClientFunctionalityTests
{
    protected override void AddQueryClient<THandler>(IServiceCollection services,
                                                     Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        _ = services.AddConquerorQueryClient<THandler>(transportClientFactory);
    }
}

[TestFixture]
public sealed class QueryClientFunctionalityWithAsyncFactoryTests : QueryClientFunctionalityTests
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
