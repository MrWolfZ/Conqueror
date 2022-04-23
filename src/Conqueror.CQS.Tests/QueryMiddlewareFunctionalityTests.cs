using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class QueryMiddlewareFunctionalityTests
    {
        [Test]
        public async Task GivenHandlerWithNoHandlerMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithoutMiddlewares>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.Empty);
            Assert.That(observations.MiddlewareTypes, Is.Empty);
        }

        [Test]
        public async Task GivenHandlerWithSingleAppliedHandlerMiddleware_MiddlewareIsCalledWithQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithSingleMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
        }

        [Test]
        public async Task GivenHandlerWithSingleAppliedHandlerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithSingleMiddlewareWithParameter>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(10), CancellationToken.None);

            Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestQueryMiddlewareConfiguration { Parameter = 10 } }));
        }

        [Test]
        public async Task GivenHandlerWithMultipleAppliedHandlerMiddlewares_MiddlewaresAreCalledWithQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2) }));
        }

        [Test]
        public async Task GivenHandlerWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithRetryMiddleware>()
                        .AddTransient<TestQueryRetryMiddleware>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var query = new TestQuery(10);

            _ = await handler.ExecuteQuery(query, CancellationToken.None);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query, query, query, query }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestQueryRetryMiddleware), typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2),
                        }));
        }

        [Test]
        public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteQuery(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheQuery()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestQueryMiddleware>()
                        .AddTransient<MutatingTestQueryMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(0), CancellationToken.None);

            var query1 = new TestQuery(0);
            var query2 = new TestQuery(1);
            var query3 = new TestQuery(3);

            Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query1, query2 }));
            Assert.That(observations.QueriesFromHandlers, Is.EquivalentTo(new[] { query3 }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestQueryMiddleware>()
                        .AddTransient<MutatingTestQueryMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response = await handler.ExecuteQuery(new(0), CancellationToken.None);

            var response1 = new TestQueryResponse(0);
            var response2 = new TestQueryResponse(1);
            var response3 = new TestQueryResponse(3);

            Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
            Assert.AreEqual(response3, response);
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestQueryMiddleware>()
                        .AddTransient<MutatingTestQueryMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler.ExecuteQuery(new(0), tokens.CancellationTokens[0]);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
            Assert.That(observations.CancellationTokensFromHandlers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
        }

        [Test]
        public void InvalidMiddlewares()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
        }

        private sealed record TestQuery(int Payload);

        private sealed record TestQueryResponse(int Payload);

        private sealed class TestQueryHandlerWithSingleMiddleware : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithSingleMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.QueriesFromHandlers.Add(query);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(query.Payload + 1);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new());
            }
        }

        private sealed class TestQueryHandlerWithSingleMiddlewareWithParameter : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithSingleMiddlewareWithParameter(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.QueriesFromHandlers.Add(query);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(query.Payload + 1);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new() { Parameter = 10 });
            }
        }

        private sealed class TestQueryHandlerWithMultipleMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithMultipleMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.QueriesFromHandlers.Add(query);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                            .Use<TestQueryMiddleware2>();
            }
        }

        private sealed class TestQueryHandlerWithoutMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithoutMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.QueriesFromHandlers.Add(query);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }
        }

        private sealed class TestQueryHandlerWithRetryMiddleware : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.QueriesFromHandlers.Add(query);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestQueryRetryMiddleware>()
                            .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                            .Use<TestQueryMiddleware2>();
            }
        }

        private sealed class TestQueryHandlerWithMultipleMutatingMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestQueryHandlerWithMultipleMutatingMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.QueriesFromHandlers.Add(query);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
            {
                _ = pipeline.Use<MutatingTestQueryMiddleware>()
                            .Use<MutatingTestQueryMiddleware2>();
            }
        }

        private sealed record TestQueryMiddlewareConfiguration
        {
            public int Parameter { get; set; }
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
        {
            private readonly TestObservations observations;

            public TestQueryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryMiddleware2 : IQueryMiddleware
        {
            private readonly TestObservations observations;

            public TestQueryMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryRetryMiddleware : IQueryMiddleware
        {
            private readonly TestObservations observations;

            public TestQueryRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                _ = await ctx.Next(ctx.Query, ctx.CancellationToken);
                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class MutatingTestQueryMiddleware : IQueryMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestQueryMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                var query = ctx.Query;

                if (query is TestQuery testQuery)
                {
                    query = (TQuery)(object)(testQuery with { Payload = testQuery.Payload + 1 });
                }

                var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[1]);

                observations.ResponsesFromMiddlewares.Add(response!);

                if (response is TestQueryResponse testQueryResponse)
                {
                    response = (TResponse)(object)(testQueryResponse with { Payload = testQueryResponse.Payload + 2 });
                }

                return response;
            }
        }

        private sealed class MutatingTestQueryMiddleware2 : IQueryMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestQueryMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.QueriesFromMiddlewares.Add(ctx.Query);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                var query = ctx.Query;

                if (query is TestQuery testQuery)
                {
                    query = (TQuery)(object)(testQuery with { Payload = testQuery.Payload + 2 });
                }

                var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[2]);

                observations.ResponsesFromMiddlewares.Add(response!);

                if (response is TestQueryResponse testQueryResponse)
                {
                    response = (TResponse)(object)(testQueryResponse with { Payload = testQueryResponse.Payload + 1 });
                }

                return response;
            }
        }

        private sealed class TestQueryMiddlewareWithMultipleInterfaces : IQueryMiddleware<TestQueryMiddlewareConfiguration>,
                                                                         IQueryMiddleware
        {
            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
                where TQuery : class =>
                throw new InvalidOperationException("this middleware should never be called");

            public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
                where TQuery : class =>
                throw new InvalidOperationException("this middleware should never be called");
        }

        private sealed class TestObservations
        {
            public List<Type> MiddlewareTypes { get; } = new();

            public List<object> QueriesFromHandlers { get; } = new();

            public List<object> QueriesFromMiddlewares { get; } = new();

            public List<object> ResponsesFromMiddlewares { get; } = new();

            public List<CancellationToken> CancellationTokensFromHandlers { get; } = new();

            public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

            public List<object> ConfigurationFromMiddlewares { get; } = new();
        }

        private sealed class CancellationTokensToUse
        {
            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
