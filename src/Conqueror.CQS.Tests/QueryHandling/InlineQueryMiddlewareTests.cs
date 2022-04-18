using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class InlineQueryMiddlewareTests
    {
        [Test]
        public async Task TransientMiddlewareForTransientQueryHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddTransient<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(6, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task TransientMiddlewareForScopedQueryHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestQueryHandlerWithMiddleware>()
                                                  .AddTransient<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(6, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task TransientMiddlewareForSingletonQueryHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestQueryHandlerWithMiddleware>()
                                                  .AddTransient<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(6, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task ScopedMiddlewareForTransientQueryHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddScoped<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task ScopedMiddlewareForScopedQueryHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestQueryHandlerWithMiddleware>()
                                                  .AddScoped<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task ScopedMiddlewareForSingletonQueryHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestQueryHandlerWithMiddleware>()
                                                  .AddScoped<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task SingletonMiddlewareForTransientQueryHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddSingleton<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(8, response3.Payload);
        }

        [Test]
        public async Task SingletonMiddlewareForScopedQueryHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestQueryHandlerWithMiddleware>()
                                                  .AddSingleton<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(8, response3.Payload);
        }

        [Test]
        public async Task SingletonMiddlewareForSingletonQueryHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestQueryHandlerWithMiddleware>()
                                                  .AddSingleton<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(8, response3.Payload);
        }

        [Test]
        public void CorrectCancellationToken()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddTransient<TestQueryMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            _ = Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteQuery(new() { Payload = 2 }, tokenSource.Token));
        }

        [Test]
        public async Task OnlySpecifiedMiddlewares()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddTransient<TestQueryMiddleware>()
                                                  .AddTransient<TestQueryMiddlewareThatShouldNeverBeCalled>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            var response = await handler.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response.Payload);
        }

        [Test]
        public void MissingMiddleware()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandlerWithMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None));
        }

        [Test]
        public void InvalidMiddlewares()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
        }
    }
}
