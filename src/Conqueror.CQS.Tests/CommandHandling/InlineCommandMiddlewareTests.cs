using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class InlineCommandMiddlewareTests
    {
        [Test]
        public async Task TransientMiddlewareForTransientCommandHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(6, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task TransientMiddlewareForTransientCommandHandlerWithoutResponse()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithoutResponseWithMiddleware>()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddSingleton<TestCommandResponses>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand>>();

            await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            var responses = provider.GetRequiredService<TestCommandResponses>().Responses;

            Assert.AreEqual(7, responses[0]);
            Assert.AreEqual(7, responses[1]);
            Assert.AreEqual(7, responses[2]);
        }

        [Test]
        public async Task TransientMiddlewareForScopedCommandHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandlerWithMiddleware>()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(6, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task TransientMiddlewareForSingletonCommandHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandlerWithMiddleware>()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(6, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task ScopedMiddlewareForTransientCommandHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddScoped<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task ScopedMiddlewareForScopedCommandHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandlerWithMiddleware>()
                                                  .AddScoped<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task ScopedMiddlewareForSingletonCommandHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandlerWithMiddleware>()
                                                  .AddScoped<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(6, response3.Payload);
        }

        [Test]
        public async Task SingletonMiddlewareForTransientCommandHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddSingleton<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(8, response3.Payload);
        }

        [Test]
        public async Task SingletonMiddlewareForScopedCommandHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandlerWithMiddleware>()
                                                  .AddSingleton<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(8, response3.Payload);
        }

        [Test]
        public async Task SingletonMiddlewareForSingletonCommandHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandlerWithMiddleware>()
                                                  .AddSingleton<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response1.Payload);
            Assert.AreEqual(7, response2.Payload);
            Assert.AreEqual(8, response3.Payload);
        }

        [Test]
        public void CorrectCancellationToken()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            _ = Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteCommand(new() { Payload = 2 }, tokenSource.Token));
        }

        [Test]
        public async Task OnlySpecifiedMiddlewares()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddTransient<TestCommandMiddleware>()
                                                  .AddTransient<TestCommandMiddlewareThatShouldNeverBeCalled>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response = await handler.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(6, response.Payload);
        }

        [Test]
        public void MissingMiddleware()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithMiddleware>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None));
        }

        [Test]
        public void InvalidMiddlewares()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
        }
    }
}
