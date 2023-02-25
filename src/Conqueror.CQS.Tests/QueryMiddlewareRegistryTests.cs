namespace Conqueror.CQS.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for assembly scanning to work")]
    public sealed class QueryMiddlewareRegistryTests
    {
        [Test]
        public void GivenManuallyRegisteredQueryMiddleware_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorQueryMiddleware<TestQueryMiddleware>()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryMiddlewareRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryMiddlewareRegistration(typeof(TestQueryMiddleware), typeof(TestQueryMiddlewareConfiguration)),
            };

            var registrations = registry.GetQueryMiddlewareRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredQueryMiddlewareWithoutConfiguration_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorQueryMiddleware<TestQueryMiddlewareWithoutConfiguration>()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryMiddlewareRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryMiddlewareRegistration(typeof(TestQueryMiddlewareWithoutConfiguration), null),
            };

            var registrations = registry.GetQueryMiddlewareRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenMultipleManuallyRegisteredQueryMiddlewares_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorQueryMiddleware<TestQueryMiddleware>()
                                                  .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryMiddlewareRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryMiddlewareRegistration(typeof(TestQueryMiddleware), typeof(TestQueryMiddlewareConfiguration)),
                new QueryMiddlewareRegistration(typeof(TestQueryMiddleware2), null),
            };

            var registrations = registry.GetQueryMiddlewareRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenQueryMiddlewaresRegisteredViaAssemblyScanning_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryMiddlewareRegistry>();

            var registrations = registry.GetQueryMiddlewareRegistrations();

            Assert.That(registrations, Contains.Item(new QueryMiddlewareRegistration(typeof(TestQueryMiddleware), typeof(TestQueryMiddlewareConfiguration))));
            Assert.That(registrations, Contains.Item(new QueryMiddlewareRegistration(typeof(TestQueryMiddleware2), null)));
        }

        public sealed class TestQueryMiddlewareConfiguration
        {
        }

        public sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        public sealed class TestQueryMiddlewareWithoutConfiguration : IQueryMiddleware
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }

        public sealed class TestQueryMiddleware2 : IQueryMiddleware
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class =>
                ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }
}
