using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests
{
    [TestFixture]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public for dynamic type generation and assembly scanning to work")]
    public sealed class QueryHandlerRegistryTests
    {
        [Test]
        public void GivenManuallyRegisteredQueryHandler_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler)),
            };

            var registrations = registry.GetQueryHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredQueryHandlerWithCustomInterface_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithCustomInterface>()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryHandlerRegistration(typeof(TestQueryWithCustomInterface), typeof(TestQueryResponse), typeof(TestQueryHandlerWithCustomInterface)),
            };

            var registrations = registry.GetQueryHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenManuallyRegisteredQueryHandlerDelegate_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((_, _, _) => Task.FromResult(new TestQueryResponse()))
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(DelegateQueryHandler<TestQuery, TestQueryResponse>)),
            };

            var registrations = registry.GetQueryHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenMultipleManuallyRegisteredQueryHandlers_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorQueryHandler<TestQueryHandler>()
                                                  .AddConquerorQueryHandler<TestQuery2Handler>()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

            var expectedRegistrations = new[]
            {
                new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler)),
                new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler)),
            };

            var registrations = registry.GetQueryHandlerRegistrations();

            Assert.That(registrations, Is.EquivalentTo(expectedRegistrations));
        }

        [Test]
        public void GivenQueryHandlersRegisteredViaAssemblyScanning_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQSTypesFromExecutingAssembly()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

            var registrations = registry.GetQueryHandlerRegistrations();

            Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler))));
            Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler))));
        }

        public sealed record TestQuery;

        public sealed record TestQueryResponse;

        public sealed record TestQueryWithCustomInterface;

        public sealed record TestQuery2;

        public sealed record TestQuery2Response;

        public interface ITestQueryHandler : IQueryHandler<TestQueryWithCustomInterface, TestQueryResponse>
        {
        }

        public sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        public sealed class TestQueryHandlerWithCustomInterface : ITestQueryHandler
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQueryWithCustomInterface query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQueryResponse());
        }

        public sealed class TestQuery2Handler : IQueryHandler<TestQuery2, TestQuery2Response>
        {
            public Task<TestQuery2Response> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default) => Task.FromResult(new TestQuery2Response());
        }
    }
}
