using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandMiddlewareLifetimeTests
    {
        [Test]
        public async Task GivenTransientMiddleware_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddleware_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddScoped<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonMiddleware_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddTransient<TestCommandHandler2>()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
        }

        [Test]
        public async Task GivenMultipleTransientMiddlewares_ResolvingHandlerCreatesNewInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithMultipleMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenMultipleScopedMiddlewares_ResolvingHandlerCreatesNewInstancesForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithMultipleMiddlewares>()
                        .AddScoped<TestCommandMiddleware>()
                        .AddScoped<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 1, 1, 2, 2, 3, 3 }));
        }

        [Test]
        public async Task GivenMultipleSingletonMiddlewares_ResolvingHandlerReturnsSameInstancesEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithMultipleMiddlewares>()
                        .AddSingleton<TestCommandMiddleware>()
                        .AddSingleton<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 }));
        }

        [Test]
        public async Task GivenMultipleMiddlewaresWithDifferentLifetimes_ResolvingHandlerReturnsInstancesAccordingToEachLifetime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares2>()
                        .AddTransient<TestCommandHandlerWithoutResponseWithMultipleMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler4 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler5 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler6 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler7 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);
            await handler4.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler5.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler6.ExecuteCommand(new(), CancellationToken.None);
            await handler7.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 6, 1, 7 }));
        }

        [Test]
        public async Task GivenTransientMiddlewareWithRetryMiddleware_EachMiddlewareExecutionGetsNewInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithRetryMiddleware>()
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithRetryMiddleware>()
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddScoped<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 1, 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonMiddlewareWithRetryMiddleware_EachMiddlewareExecutionUsesInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithRetryMiddleware>()
                        .AddTransient<TestCommandRetryMiddleware>()
                        .AddSingleton<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1, 2, 1, 1, 3, 1, 4, 1 }));
        }

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private sealed record TestCommand2;

        private sealed record TestCommandResponse2;

        private sealed record TestCommandWithoutResponse;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            [TestCommandMiddleware]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
        {
            [TestCommandMiddleware]
            public async Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            [TestCommandMiddleware]
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
            }
        }

        private sealed class TestCommandHandlerWithMultipleMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>
        {
            [TestCommandMiddleware]
            [TestCommandMiddleware2]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestCommandHandlerWithMultipleMiddlewares2 : ICommandHandler<TestCommand2, TestCommandResponse2>
        {
            [TestCommandMiddleware]
            [TestCommandMiddleware2]
            public async Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithMultipleMiddlewares : ICommandHandler<TestCommandWithoutResponse>
        {
            [TestCommandMiddleware]
            [TestCommandMiddleware2]
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
            }
        }

        private sealed class TestCommandHandlerWithRetryMiddleware : ICommandHandler<TestCommand, TestCommandResponse>
        {
            [TestCommandRetryMiddleware]
            [TestCommandMiddleware]
            [TestCommandMiddleware2]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                return new();
            }
        }

        private sealed class TestCommandMiddlewareAttribute : CommandMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestCommandMiddleware2Attribute : CommandMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestCommandRetryMiddlewareAttribute : CommandMiddlewareConfigurationAttribute
        {
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareAttribute>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareAttribute> ctx)
                where TCommand : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandMiddleware2 : ICommandMiddleware<TestCommandMiddleware2Attribute>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddleware2Attribute> ctx)
                where TCommand : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandRetryMiddleware : ICommandMiddleware<TestCommandRetryMiddlewareAttribute>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandRetryMiddlewareAttribute> ctx)
                where TCommand : class
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);

                _ = await ctx.Next(ctx.Command, ctx.CancellationToken);
                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestObservations
        {
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
