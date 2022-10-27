namespace Conqueror.CQS.Tests
{
    public sealed class QueryClientFunctionalityTests
    {
        [Test]
        public async Task GivenQuery_TransportReceivesQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>())
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>())
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>())
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var client = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            var response = await client.ExecuteQuery(query, CancellationToken.None);

            Assert.AreEqual(query.Payload + 1, response.Payload);
        }

        [Test]
        public async Task GivenScopedFactory_TransportIsResolvedOnSameScope()
        {
            var seenInstances = new List<TestQueryTransport>();

            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b =>
                        {
                            var transport = b.ServiceProvider.GetRequiredService<TestQueryTransport>();
                            seenInstances.Add(transport);
                            return transport;
                        })
                        .AddScoped<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var client1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var client2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var client3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await client1.ExecuteQuery(new(10), CancellationToken.None);
            _ = await client2.ExecuteQuery(new(10), CancellationToken.None);
            _ = await client3.ExecuteQuery(new(10), CancellationToken.None);

            Assert.That(seenInstances, Has.Count.EqualTo(3));
            Assert.AreSame(seenInstances[0], seenInstances[1]);
            Assert.AreNotSame(seenInstances[0], seenInstances[2]);
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed class TestQueryTransport : IQueryTransportClient
        {
            private readonly TestObservations observations;

            public TestQueryTransport(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
                where TQuery : class
            {
                await Task.Yield();
                observations.Queries.Add(query);
                observations.CancellationTokens.Add(cancellationToken);

                var cmd = (TestQuery)(object)query;
                return (TResponse)(object)new TestQueryResponse(cmd.Payload + 1);
            }
        }

        private sealed class TestObservations
        {
            public List<object> Queries { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
