namespace Conqueror.CQS.Tests
{
    public sealed class QueryHandlerFunctionalityTests
    {
        [Test]
        public async Task GivenQuery_HandlerReceivesQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
        }

        [Test]
        public async Task GivenGenericQuery_HandlerReceivesQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorQueryHandler<GenericTestQueryHandler<string>>()
                        .AddSingleton(observations);

            var provider = services.BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<GenericTestQuery<string>, GenericTestQueryResponse<string>>>();

            var query = new GenericTestQuery<string>("test string");

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
        }

        [Test]
        public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteQuery(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(10));

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
        }

        [Test]
        public async Task GivenQuery_HandlerReturnsResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            var response = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.AreEqual(query.Payload + 1, response.Payload);
        }

        [Test]
        public void GivenExceptionInHandler_InvocationThrowsSameException()
        {
            var services = new ServiceCollection();
            var exception = new Exception();

            _ = services.AddConquerorQueryHandler<ThrowingQueryHandler>()
                        .AddSingleton(exception);

            var provider = services.BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

            Assert.AreSame(exception, thrownException);
        }

        [Test]
        public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithoutValidInterfaces>());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithoutValidInterfaces>(_ => new()));
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler(new TestQueryHandlerWithoutValidInterfaces()));
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed record GenericTestQuery<T>(T Payload);

        private sealed record GenericTestQueryResponse<T>(T Payload);

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Queries.Add(query);
                observations.CancellationTokens.Add(cancellationToken);
                return new(query.Payload + 1);
            }
        }

        private sealed class GenericTestQueryHandler<T> : IQueryHandler<GenericTestQuery<T>, GenericTestQueryResponse<T>>
        {
            private readonly TestObservations observations;

            public GenericTestQueryHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<GenericTestQueryResponse<T>> ExecuteQuery(GenericTestQuery<T> query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Queries.Add(query);
                observations.CancellationTokens.Add(cancellationToken);
                return new(query.Payload);
            }
        }

        private sealed class ThrowingQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly Exception exception;

            public ThrowingQueryHandler(Exception exception)
            {
                this.exception = exception;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                throw exception;
            }
        }

        private sealed class TestQueryHandlerWithoutValidInterfaces : IQueryHandler
        {
        }

        private sealed class TestObservations
        {
            public List<object> Queries { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
