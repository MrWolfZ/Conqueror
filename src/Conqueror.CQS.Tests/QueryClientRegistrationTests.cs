namespace Conqueror.CQS.Tests
{
    public sealed class QueryClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryHandler<UnregisteredTestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestQueryHandler>());
        }

        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport())
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryClient<ITestQueryHandlerWithExtraMethod>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenRegisteredAndConfiguredHandler_WhenRegisteringPlainClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestQueryHandler>().ConfigureConqueror();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenRegisteredAndConfiguredHandler_WhenRegisteringCustomClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestQueryHandler>().ConfigureConqueror();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenRegisteredHandlerAndPlainClient_WhenConfiguring_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestQueryHandler>().AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenRegisteredHandlerAndCustomClient_WhenConfiguring_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorCQS().AddTransient<TestQueryHandler>().AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenClient_CanResolveConquerorContextAccessor()
        {
            using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
        }

        private ServiceProvider RegisterClient<TQueryHandler>()
            where TQueryHandler : class, IQueryHandler
        {
            return new ServiceCollection().AddConquerorCQS()
                                          .AddConquerorQueryClient<TQueryHandler>(_ => new TestQueryTransport())
                                          .ConfigureConqueror()
                                          .BuildServiceProvider();
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        public sealed record TestQueryWithoutResponse;

        public sealed record UnregisteredTestQuery;

        public sealed record UnregisteredTestQueryWithoutResponse;

        public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
        }

        public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
        {
            void ExtraMethod();
        }

        public interface IUnregisteredTestQueryHandler : IQueryHandler<UnregisteredTestQuery, TestQueryResponse>
        {
        }

        private sealed class TestQueryTransport : IQueryTransportClient
        {
            public async Task<TResponse> ExecuteQuery<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
                where TQuery : class
            {
                await Task.Yield();

                throw new NotSupportedException("should never be called");
            }
        }

        private sealed class TestQueryHandler : ITestQueryHandler
        {
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();

                throw new NotSupportedException("should never be called");
            }
        }
    }
}
