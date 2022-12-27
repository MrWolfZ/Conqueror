namespace Conqueror.CQS.Tests
{
    public sealed class QueryClientCustomInterfaceTests
    {
        [Test]
        public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorQueryClient<ITestQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>())
                        .AddTransient<TestQueryTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestQueryHandler>();

            var query = new TestQuery();

            _ = await client.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
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
        }
    }
}
