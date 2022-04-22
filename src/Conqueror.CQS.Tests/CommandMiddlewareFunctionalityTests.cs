using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandMiddlewareFunctionalityTests
    {
        [Test]
        public async Task GivenHandlerWithNoHandlerMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.Empty);
            Assert.That(observations.MiddlewareTypes, Is.Empty);
        }

        [Test]
        public async Task GivenHandlerWithoutResponseNoHandlerMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var command = new TestCommandWithoutResponse(10);

            await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.Empty);
            Assert.That(observations.MiddlewareTypes, Is.Empty);
        }

        [Test]
        public async Task GivenHandlerWithSingleAppliedHandlerMiddleware_MiddlewareIsCalledWithCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithSingleMiddleware>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
        }

        [Test]
        public async Task GivenHandlerWithSingleAppliedHandlerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithSingleMiddlewareWithParameter>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(10), CancellationToken.None);

            Assert.That(observations.AttributesFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareAttribute { Parameter = 10 } }));
        }

        [Test]
        public async Task GivenHandlerWithoutResponseWithSingleAppliedHandlerMiddleware_MiddlewareIsCalledWithCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithSingleMiddleware>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var command = new TestCommandWithoutResponse(10);

            await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
        }

        [Test]
        public async Task GivenHandlerWithoutResponseWithSingleAppliedHandlerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(10), CancellationToken.None);

            Assert.That(observations.AttributesFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareAttribute { Parameter = 10 } }));
        }

        [Test]
        public async Task GivenHandlerWithMultipleAppliedHandlerMiddlewares_MiddlewaresAreCalledWithCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2) }));
        }

        [Test]
        public async Task GivenHandlerWithoutResponseWithMultipleAppliedHandlerMiddlewares_MiddlewaresAreCalledWithCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithMultipleMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var command = new TestCommandWithoutResponse(10);

            await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2) }));
        }

        [Test]
        public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteCommand(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestCommandMiddleware>()
                        .AddTransient<MutatingTestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(0), CancellationToken.None);

            var command1 = new TestCommand(0);
            var command2 = new TestCommand(1);
            var command3 = new TestCommand(3);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command1, command2 }));
            Assert.That(observations.CommandsFromHandlers, Is.EquivalentTo(new[] { command3 }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheCommandWithoutResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestCommandMiddleware>()
                        .AddTransient<MutatingTestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler.ExecuteCommand(new(0), CancellationToken.None);

            var command1 = new TestCommandWithoutResponse(0);
            var command2 = new TestCommandWithoutResponse(1);
            var command3 = new TestCommandWithoutResponse(3);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command1, command2 }));
            Assert.That(observations.CommandsFromHandlers, Is.EquivalentTo(new[] { command3 }));
        }

        [Test]
        public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestCommandMiddleware>()
                        .AddTransient<MutatingTestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var response = await handler.ExecuteCommand(new(0), CancellationToken.None);

            var response1 = new TestCommandResponse(0);
            var response2 = new TestCommandResponse(1);
            var response3 = new TestCommandResponse(3);

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
                        .AddTransient<TestCommandHandlerWithMultipleMutatingMiddlewares>()
                        .AddTransient<MutatingTestCommandMiddleware>()
                        .AddTransient<MutatingTestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .AddSingleton(tokens);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler.ExecuteCommand(new(0), tokens.CancellationTokens[0]);

            Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
            Assert.That(observations.CancellationTokensFromHandlers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
        }

        [Test]
        public void InvalidMiddlewares()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandMiddlewareWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandMiddlewareWithoutInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandMiddlewareWithoutInterfaces>().ConfigureConqueror());
        }

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int Payload);

        private sealed record TestCommandWithoutResponse(int Payload);

        private sealed class TestCommandHandlerWithSingleMiddleware : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithSingleMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestCommandMiddleware]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(command.Payload + 1);
            }
        }

        private sealed class TestCommandHandlerWithSingleMiddlewareWithParameter : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithSingleMiddlewareWithParameter(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestCommandMiddleware(Parameter = 10)]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(command.Payload + 1);
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithSingleMiddleware : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithSingleMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestCommandMiddleware]
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestCommandMiddleware(Parameter = 10)]
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }
        }

        private sealed class TestCommandHandlerWithMultipleMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithMultipleMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestCommandMiddleware]
            [TestCommandMiddleware2]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithMultipleMiddlewares : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithMultipleMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            [TestCommandMiddleware]
            [TestCommandMiddleware2]
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }
        }

        private sealed class TestCommandHandlerWithoutMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithoutMiddlewares : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithoutMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }
        }

        private sealed class TestCommandHandlerWithMultipleMutatingMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithMultipleMutatingMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            [MutatingTestCommandMiddleware]
            [MutatingTestCommandMiddleware2]
            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(0);
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            [MutatingTestCommandMiddleware]
            [MutatingTestCommandMiddleware2]
            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }
        }

        private sealed class TestCommandMiddlewareAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<TestCommandMiddleware>
        {
            public int Parameter { get; set; }
        }

        private sealed class TestCommandMiddleware2Attribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<TestCommandMiddleware2>
        {
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareAttribute>
        {
            private readonly TestObservations observations;

            public TestCommandMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareAttribute> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandMiddleware2 : ICommandMiddleware<TestCommandMiddleware2Attribute>
        {
            private readonly TestObservations observations;

            public TestCommandMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddleware2Attribute> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class MutatingTestCommandMiddlewareAttribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<MutatingTestCommandMiddleware>
        {
        }

        private sealed class MutatingTestCommandMiddleware2Attribute : CommandMiddlewareConfigurationAttribute, ICommandMiddlewareConfiguration<MutatingTestCommandMiddleware2>
        {
        }

        private sealed class MutatingTestCommandMiddleware : ICommandMiddleware<MutatingTestCommandMiddlewareAttribute>
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestCommandMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, MutatingTestCommandMiddlewareAttribute> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                var command = ctx.Command;

                if (command is TestCommand testCommand)
                {
                    command = (TCommand)(object)(testCommand with { Payload = testCommand.Payload + 1 });
                }

                if (command is TestCommandWithoutResponse testCommandWithoutResponse)
                {
                    command = (TCommand)(object)(testCommandWithoutResponse with { Payload = testCommandWithoutResponse.Payload + 1 });
                }

                var response = await ctx.Next(command, cancellationTokensToUse.CancellationTokens[1]);

                observations.ResponsesFromMiddlewares.Add(response!);

                if (response is TestCommandResponse testCommandResponse)
                {
                    response = (TResponse)(object)(testCommandResponse with { Payload = testCommandResponse.Payload + 2 });
                }

                return response;
            }
        }

        private sealed class MutatingTestCommandMiddleware2 : ICommandMiddleware<MutatingTestCommandMiddleware2Attribute>
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestCommandMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, MutatingTestCommandMiddleware2Attribute> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.AttributesFromMiddlewares.Add(ctx.Configuration);

                var command = ctx.Command;

                if (command is TestCommand testCommand)
                {
                    command = (TCommand)(object)(testCommand with { Payload = testCommand.Payload + 2 });
                }

                if (command is TestCommandWithoutResponse testCommandWithoutResponse)
                {
                    command = (TCommand)(object)(testCommandWithoutResponse with { Payload = testCommandWithoutResponse.Payload + 2 });
                }

                var response = await ctx.Next(command, cancellationTokensToUse.CancellationTokens[2]);

                observations.ResponsesFromMiddlewares.Add(response!);

                if (response is TestCommandResponse testCommandResponse)
                {
                    response = (TResponse)(object)(testCommandResponse with { Payload = testCommandResponse.Payload + 1 });
                }

                return response;
            }
        }

        private sealed class TestCommandMiddlewareWithMultipleInterfaces : ICommandMiddleware<TestCommandMiddlewareAttribute>,
                                                                           ICommandMiddleware<TestCommandMiddleware2Attribute>
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddleware2Attribute> ctx)
                where TCommand : class =>
                throw new InvalidOperationException("this middleware should never be called");

            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareAttribute> ctx)
                where TCommand : class =>
                throw new InvalidOperationException("this middleware should never be called");
        }

        private sealed class TestCommandMiddlewareWithoutInterfaces : ICommandMiddleware
        {
        }

        private sealed class TestObservations
        {
            public List<Type> MiddlewareTypes { get; } = new();

            public List<object> CommandsFromHandlers { get; } = new();

            public List<object> CommandsFromMiddlewares { get; } = new();

            public List<object> ResponsesFromMiddlewares { get; } = new();

            public List<CancellationToken> CancellationTokensFromHandlers { get; } = new();

            public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

            public List<CommandMiddlewareConfigurationAttribute> AttributesFromMiddlewares { get; } = new();
        }

        private sealed class CancellationTokensToUse
        {
            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
