namespace Conqueror.CQS.Tests
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
    public sealed class QueryClientRegistrationTests
    {
        [Test]
        public void GivenRegisteredPlainClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolvePlainClient()
        {
            using var provider = RegisterClient<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolvePlainClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenRegisteredCustomClient_CanResolveCustomClient()
        {
            using var provider = RegisterClient<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClient()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestQueryHandler>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public void GivenUnregisteredPlainClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryHandler<UnregisteredTestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenUnregisteredPlainClientWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IQueryHandler<UnregisteredTestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenUnregisteredCustomClient_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClient<ITestQueryHandler>();
            _ = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IUnregisteredTestQueryHandler>());
        }

        [Test]
        public void GivenUnregisteredCustomClientWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            using var provider = RegisterClientWithAsyncClientFactory<ITestQueryHandler>();
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
        public void GivenRegisteredPlainClientWithAsyncClientFactory_CanResolvePlainClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
                                                  {
                                                      await Task.CompletedTask;
                                                      return new TestQueryTransport();
                                                  })
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
        public void GivenRegisteredCustomClientWithAsyncClientFactory_CanResolveCustomClientWithoutHavingServicesExplicitlyRegistered()
        {
            var provider = new ServiceCollection().AddConquerorQueryClient<ITestQueryHandler>(async _ =>
                                                  {
                                                      await Task.CompletedTask;
                                                      return new TestQueryTransport();
                                                  })
                                                  .BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClient_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredPlainClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClient_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegistering_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClient_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport());

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredCustomClientWithAsyncClientFactory_WhenRegisteringWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            });

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryHandler<TestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredHandler_WhenRegisteringPlainClientWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryHandler<TestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClient_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryHandler<TestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenAlreadyRegisteredHandler_WhenRegisteringCustomClientWithAsyncClientFactory_ThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            _ = services.AddConquerorQueryHandler<TestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.AddConquerorQueryClient<ITestQueryHandler>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenCustomInterfaceWithExtraMethods_WhenRegistering_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            _ = services;

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryClient<ITestQueryHandlerWithExtraMethod>(_ => new TestQueryTransport()));
        }

        [Test]
        public void GivenCustomInterfaceWithExtraMethods_WhenRegisteringWithAsyncClientFactory_ThrowsArgumentException()
        {
            var services = new ServiceCollection();
            _ = services;

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryClient<ITestQueryHandlerWithExtraMethod>(async _ =>
            {
                await Task.CompletedTask;
                return new TestQueryTransport();
            }));
        }

        [Test]
        public void GivenClient_CanResolveConquerorContextAccessor()
        {
            using var provider = RegisterClient<IQueryHandler<TestQuery, TestQueryResponse>>();

            Assert.DoesNotThrow(() => provider.GetRequiredService<IConquerorContextAccessor>());
        }

        private static ServiceProvider RegisterClient<TQueryHandler>()
            where TQueryHandler : class, IQueryHandler
        {
            return new ServiceCollection().AddConquerorQueryClient<TQueryHandler>(_ => new TestQueryTransport())
                                          .BuildServiceProvider();
        }

        private static ServiceProvider RegisterClientWithAsyncClientFactory<TQueryHandler>()
            where TQueryHandler : class, IQueryHandler
        {
            return new ServiceCollection().AddConquerorQueryClient<TQueryHandler>(_ => new TestQueryTransport())
                                          .BuildServiceProvider();
        }

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
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();

                throw new NotSupportedException("should never be called");
            }
        }
    }
}
