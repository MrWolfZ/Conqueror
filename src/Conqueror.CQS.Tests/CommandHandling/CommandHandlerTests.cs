using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests.CommandHandling
{
    public sealed class CommandHandlerTests
    {
        [Test]
        public async Task TransientCommandHandler()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandler>()
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

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(3, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task TransientCommandHandlerWithoutResponse()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithoutResponse>()
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

            Assert.AreEqual(3, responses[0]);
            Assert.AreEqual(3, responses[1]);
            Assert.AreEqual(3, responses[2]);
        }

        [Test]
        public async Task TransientCommandHandlerWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(3, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task TransientCommandHandlerWithoutResponseWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithoutResponse>()
                                                  .AddSingleton<TestCommandResponses>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

            await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            var responses = provider.GetRequiredService<TestCommandResponses>().Responses;

            Assert.AreEqual(3, responses[0]);
            Assert.AreEqual(3, responses[1]);
            Assert.AreEqual(3, responses[2]);
        }

        [Test]
        public async Task ScopedCommandHandler()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandler>()
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

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task ScopedCommandHandlerWithoutResponse()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandlerWithoutResponse>()
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

            Assert.AreEqual(3, responses[0]);
            Assert.AreEqual(4, responses[1]);
            Assert.AreEqual(3, responses[2]);
        }

        [Test]
        public async Task ScopedCommandHandlerWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(3, response3.Payload);
        }

        [Test]
        public async Task ScopedCommandHandlerWithoutResponseWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddScoped<TestCommandHandlerWithoutResponse>()
                                                  .AddSingleton<TestCommandResponses>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

            await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            var responses = provider.GetRequiredService<TestCommandResponses>().Responses;

            Assert.AreEqual(3, responses[0]);
            Assert.AreEqual(4, responses[1]);
            Assert.AreEqual(3, responses[2]);
        }

        [Test]
        public async Task SingletonCommandHandler()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandler>()
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

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(5, response3.Payload);
        }

        [Test]
        public async Task SingletonCommandHandlerWithoutResponse()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandlerWithoutResponse>()
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

            Assert.AreEqual(3, responses[0]);
            Assert.AreEqual(4, responses[1]);
            Assert.AreEqual(5, responses[2]);
        }

        [Test]
        public async Task SingletonCommandHandlerWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandler>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandler>();

            var response1 = await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response2 = await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            var response3 = await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            Assert.AreEqual(3, response1.Payload);
            Assert.AreEqual(4, response2.Payload);
            Assert.AreEqual(5, response3.Payload);
        }

        [Test]
        public async Task SingletonCommandHandlerWithoutResponseWithDedicatedInterface()
        {
            var provider = new ServiceCollection().AddSingleton<TestCommandHandlerWithoutResponse>()
                                                  .AddSingleton<TestCommandResponses>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

            await handler1.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler2.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);
            await handler3.ExecuteCommand(new() { Payload = 2 }, CancellationToken.None);

            var responses = provider.GetRequiredService<TestCommandResponses>().Responses;

            Assert.AreEqual(3, responses[0]);
            Assert.AreEqual(4, responses[1]);
            Assert.AreEqual(5, responses[2]);
        }

        [Test]
        public void InvalidCommandHandlers()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleCustomInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleMixedCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleMixedCustomInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleMixedCustomInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleInterfacesWithoutResponse>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleCustomInterfacesWithoutResponse>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleMixedCustomInterfacesWithoutResponse>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithoutResponseWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithMultipleMixedInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandHandlerWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandHandlerWithoutInterfaces>().ConfigureConqueror());
        }

        [Test]
        public void CorrectCancellationToken()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandler>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            _ = Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteCommand(new() { Payload = 2 }, tokenSource.Token));
        }

        [Test]
        public void CorrectCancellationTokenWithoutResponse()
        {
            var provider = new ServiceCollection().AddTransient<TestCommandHandlerWithoutResponse>()
                                                  .AddSingleton<TestCommandResponses>()
                                                  .AddConquerorCQS()
                                                  .ConfigureConqueror()
                                                  .BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand>>();
            using var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();

            _ = Assert.ThrowsAsync<OperationCanceledException>(() => handler.ExecuteCommand(new() { Payload = 2 }, tokenSource.Token));
        }
    }
}
