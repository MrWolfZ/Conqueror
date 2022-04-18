using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests.QueryHandling
{
    public sealed class QueryHandlerTests
    {
        [Test]
        public async Task TransientQueryHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandler>()
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

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(3, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task TransientQueryHandlerWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(3, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task ScopedQueryHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestQueryHandler>()
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

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task ScopedQueryHandlerWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddScoped<TestQueryHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task SingletonQueryHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestQueryHandler>()
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

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(5, response3.Payload);
        }

        [Test]
        public async Task SingletonQueryHandlerWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddSingleton<TestQueryHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestQueryHandler>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestQueryHandler>();

            var response1 = await handler1.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteQuery(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(5, response3.Payload);
        }

        [Test]
        public void InvalidQueryHandlers()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithMultipleInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestQueryHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestQueryHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestQueryHandlerWithoutInterfaces>().ConfigureConqueror());
        }

        [Test]
        public void CorrectCancellationToken()
        {
            var provider = new ServiceCollection().AddTransient<TestQueryHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            _ = Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteQuery(new() { Payload = 2 }, tokenSource.Token));
        }
    }
}
