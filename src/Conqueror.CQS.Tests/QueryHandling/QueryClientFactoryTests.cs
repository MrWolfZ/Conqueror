namespace Conqueror.CQS.Tests.QueryHandling;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public abstract class QueryClientFactoryTests
{
    [Test]
    public async Task GivenPlainHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        var client = CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        var query = new TestQuery();

        _ = await client.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        var client = CreateQueryClient<ITestQueryHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        var query = new TestQuery();

        _ = await client.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenPlainClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        var client = CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(clientFactory,
                                                                                    b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        var query = new TestQuery();

        _ = await client.WithPipeline(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                        .ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>) }));
    }

    [Test]
    public async Task GivenCustomClientWithPipeline_PipelineIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        var client = CreateQueryClient<ITestQueryHandler>(clientFactory,
                                                          b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

        var query = new TestQuery();

        _ = await client.WithPipeline(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                        .ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>) }));
    }

    [Test]
    public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<ITestQueryHandlerWithExtraMethod>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
    }

    [Test]
    public void GivenNonGenericQueryHandlerInterface_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<INonGenericQueryHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
    }

    [Test]
    public void GivenConcreteQueryHandlerType_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<TestQueryHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
    }

    [Test]
    public void GivenQueryHandlerInterfaceThatImplementsMultipleOtherPlainQueryHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<ICombinedQueryHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
    }

    [Test]
    public void GivenQueryHandlerInterfaceThatImplementsMultipleOtherCustomQueryHandlerInterfaces_CreatingClientThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorCQS()
                    .AddTransient<TestQueryTransport>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

        _ = Assert.Throws<ArgumentException>(() => CreateQueryClient<ICombinedCustomQueryHandler>(clientFactory, b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
    }

    protected abstract THandler CreateQueryClient<THandler>(IQueryClientFactory clientFactory,
                                                            Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
        where THandler : class, IQueryHandler;

    public sealed record TestQuery;

    public sealed record TestQueryResponse;

    public sealed record TestQuery2;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }

    public interface ITestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse>
    {
    }

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }

    public interface ICombinedQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>, IQueryHandler<TestQuery2, TestQueryResponse>
    {
    }

    public interface ICombinedCustomQueryHandler : ITestQueryHandler, ITestQueryHandler2
    {
    }

    public interface INonGenericQueryHandler : IQueryHandler
    {
        void SomeMethod();
    }

    private sealed class TestQueryHandler : ITestQueryHandler
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
    {
        private readonly TestObservations observations;

        public TestQueryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryTransport : IQueryTransportClient
    {
        private readonly TestObservations responses;

        public TestQueryTransport(TestObservations responses)
        {
            this.responses = responses;
        }

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
        public List<object> Queries { get; } = new();

        public List<Type> MiddlewareTypes { get; } = new();
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
public sealed class QueryClientFactoryWithSyncFactoryTests : QueryClientFactoryTests
{
    protected override THandler CreateQueryClient<THandler>(IQueryClientFactory clientFactory,
                                                            Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        return clientFactory.CreateQueryClient<THandler>(transportClientFactory);
    }
}

[TestFixture]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
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
