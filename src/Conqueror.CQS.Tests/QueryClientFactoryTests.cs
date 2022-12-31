namespace Conqueror.CQS.Tests
{
    public sealed class QueryClientFactoryTests
    {
        [Test]
        public async Task GivenPlainHandlerInterface_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            var client = clientFactory.CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            var client = clientFactory.CreateQueryClient<ITestQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());

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
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            var client = clientFactory.CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>(),
                                                                                                      p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()));

            var query = new TestQuery();

            _ = await client.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
        }

        [Test]
        public async Task GivenCustomClientWithPipeline_PipelineIsCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            var client = clientFactory.CreateQueryClient<ITestQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>(),
                                                                            p => p.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()));

            var query = new TestQuery();

            _ = await client.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
        }

        [Test]
        public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateQueryClient<ITestQueryHandlerWithExtraMethod>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
        }

        [Test]
        public void GivenNonGenericQueryHandlerInterface_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateQueryClient<INonGenericQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
        }

        [Test]
        public void GivenConcreteQueryHandlerType_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateQueryClient<TestQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
        }

        [Test]
        public void GivenQueryHandlerInterfaceThatImplementsMultipleOtherPlainQueryHandlerInterfaces_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateQueryClient<ICombinedQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
        }

        [Test]
        public void GivenQueryHandlerInterfaceThatImplementsMultipleOtherCustomQueryHandlerInterfaces_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<IQueryClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateQueryClient<ICombinedCustomQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>()));
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

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

        private sealed record TestQueryMiddlewareConfiguration;

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            private readonly TestObservations observations;

            public TestQueryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class
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

            public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
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
}
