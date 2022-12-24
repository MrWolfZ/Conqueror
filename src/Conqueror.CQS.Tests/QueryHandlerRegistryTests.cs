﻿namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class QueryHandlerRegistryTests
    {
        [Test]
        public void GivenManuallyRegisteredQueryHandler_ReturnsRegistration()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .FinalizeConquerorRegistrations()
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
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandlerWithCustomInterface>()
                                                  .FinalizeConquerorRegistrations()
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
        public void GivenMultipleManuallyRegisteredQueryHandlers_ReturnsRegistrations()
        {
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<TestQuery2Handler>()
                                                  .FinalizeConquerorRegistrations()
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
            var provider = new ServiceCollection().AddConquerorCQS()
                                                  .AddConquerorCQSTypesFromExecutingAssembly()
                                                  .FinalizeConquerorRegistrations()
                                                  .BuildServiceProvider();

            var registry = provider.GetRequiredService<IQueryHandlerRegistry>();

            var registrations = registry.GetQueryHandlerRegistrations();

            Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery), typeof(TestQueryResponse), typeof(TestQueryHandler))));
            Assert.That(registrations, Contains.Item(new QueryHandlerRegistration(typeof(TestQuery2), typeof(TestQuery2Response), typeof(TestQuery2Handler))));
        }

// types must be public for dynamic type generation and assembly scanning to work
#pragma warning disable CA1034

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
