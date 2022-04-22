using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    [TestFixture]
    public sealed class QueryHandlerServiceCollectionConfigurationTests
    {
        [Test]
        public void GivenMultipleRegisteredIdenticalHandlerTypes_ConfiguringServiceCollectionDoesNotThrow()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<TestQueryHandler>();

            Assert.DoesNotThrow(() => services.ConfigureConqueror());
        }

        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameQueryAndResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<DuplicateTestQueryHandler>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }
        
        [Test]
        public void GivenMultipleRegisteredHandlerTypesForSameQueryAndDifferentResponseTypes_ConfiguringServiceCollectionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection().AddConquerorCQS()
                                                  .AddTransient<TestQueryHandler>()
                                                  .AddTransient<DuplicateTestQueryHandlerWithDifferentResponseType>();

            _ = Assert.Throws<InvalidOperationException>(() => services.ConfigureConqueror());
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed record TestQueryResponse2;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            public Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed class DuplicateTestQueryHandlerWithDifferentResponseType : IQueryHandler<TestQuery, TestQueryResponse2>
        {
            public Task<TestQueryResponse2> ExecuteQuery(TestQuery command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}
