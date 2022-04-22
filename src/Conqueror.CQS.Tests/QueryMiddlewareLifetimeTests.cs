using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class QueryMiddlewareLifetimeTests
    {
        [Test]
        public async Task GivenTransientMiddleware_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddScoped<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddTransient<TestQueryHandler2>()
                        .AddSingleton<TestQueryMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public async Task GivenMultipleTransientMiddlewares_ResolvingHandlerCreatesNewInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddTransient<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenMultipleScopedMiddlewares_ResolvingHandlerCreatesNewInstancesForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddScoped<TestQueryMiddleware>()
                        .AddScoped<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 1, 1, 2, 2 }));
        }

        [Test]
        public async Task GivenMultipleSingletonMiddlewares_ResolvingHandlerReturnsSameInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddSingleton<TestQueryMiddleware>()
                        .AddSingleton<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 }));
        }

        [Test]
        public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingHandlerReturnsInstancesAccordingToEachLifetime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestQueryHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestQueryMiddleware>()
                        .AddSingleton<TestQueryMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler4 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler5.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5 }));
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed record TestQuery2;

        private sealed record TestQueryResponse2;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            [TestQueryMiddleware]
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
        {
            [TestQueryMiddleware]
            public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestQueryHandlerWithMultipleMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>
        {
            [TestQueryMiddleware]
            [TestQueryMiddleware2]
            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestQueryHandlerWithMultipleMiddlewares2 : IQueryHandler<TestQuery2, TestQueryResponse2>
        {
            [TestQueryMiddleware]
            [TestQueryMiddleware2]
            public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestQueryMiddlewareAttribute : QueryMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestQueryMiddleware2Attribute : QueryMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareAttribute>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareAttribute> ctx)
                where TQuery : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestQueryMiddleware2 : IQueryMiddleware<TestQueryMiddleware2Attribute>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddleware2Attribute> ctx)
                where TQuery : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                return await ctx.Next(ctx.Query, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
