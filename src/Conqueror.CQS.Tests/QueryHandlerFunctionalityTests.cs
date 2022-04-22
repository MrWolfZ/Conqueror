using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class QueryHandlerFunctionalityTests
    {
        [Test]
        public async Task GivenQuery_HandlerReceivesQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
        }

        [Test]
        public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteQuery(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }
        
        [Test]
        public async Task GivenQuery_HandlerReturnsResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            var response = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.AreEqual(query.Payload + 1, response.Payload);
        }

        [Test]
        public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithoutValidInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithoutValidInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithoutValidInterfaces>().ConfigureConqueror());
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations responses;

            public TestQueryHandler(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                responses.Queries.Add(query);
                responses.CancellationTokens.Add(cancellationToken);
                return new(query.Payload + 1);
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
