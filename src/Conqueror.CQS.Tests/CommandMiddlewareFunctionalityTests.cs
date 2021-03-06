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

            Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareConfiguration { Parameter = 10 } }));
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

            Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestCommandMiddlewareConfiguration { Parameter = 10 } }));
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
        public async Task GivenHandlerWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithCommandMultipleTimes()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .ConfigureCommandPipeline<TestCommandHandlerWithoutMiddlewares>(pipeline =>
                        {
                            _ = pipeline.Use<TestCommandMiddleware2>()
                                        .Use<TestCommandMiddleware2>();
                        });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
        }

        [Test]
        public async Task GivenHandlerWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .ConfigureCommandPipeline<TestCommandHandlerWithoutMiddlewares>(pipeline =>
                        {
                            _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                        .Use<TestCommandMiddleware2>()
                                        .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                        .Without<TestCommandMiddleware2>();
                        });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware), typeof(TestCommandMiddleware) }));
        }

        [Test]
        public async Task GivenHandlerWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddTransient<TestCommandMiddleware2>()
                        .AddSingleton(observations)
                        .ConfigureCommandPipeline<TestCommandHandlerWithoutMiddlewares>(pipeline =>
                        {
                            _ = pipeline.Use<TestCommandMiddleware2>()
                                        .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                                        .Use<TestCommandMiddleware2>()
                                        .Without<TestCommandMiddleware, TestCommandMiddlewareConfiguration>();
                        });

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command }));
            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware2) }));
        }

        [Test]
        public async Task GivenHandlerWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithCommand()
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

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.CommandsFromMiddlewares, Is.EquivalentTo(new[] { command, command, command, command, command }));
            Assert.That(observations.MiddlewareTypes,
                        Is.EquivalentTo(new[]
                        {
                            typeof(TestCommandRetryMiddleware), typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2), typeof(TestCommandMiddleware), typeof(TestCommandMiddleware2),
                        }));
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
        public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var observedInstances = new List<TestService>();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddScoped<TestService>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithoutMiddlewares>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(10), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(10), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(10), CancellationToken.None);

            Assert.That(observedInstances, Has.Count.EqualTo(3));
            Assert.AreNotSame(observedInstances[0], observedInstances[1]);
            Assert.AreSame(observedInstances[0], observedInstances[2]);
        }

        [Test]
        public async Task GivenPipelineWithoutResponseThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();
            var observedInstances = new List<TestService>();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                        .AddScoped<TestService>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithoutResponseWithoutMiddlewares>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler2 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler3 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler1.ExecuteCommand(new(10), CancellationToken.None);
            await handler2.ExecuteCommand(new(10), CancellationToken.None);
            await handler3.ExecuteCommand(new(10), CancellationToken.None);

            Assert.That(observedInstances, Has.Count.EqualTo(3));
            Assert.AreNotSame(observedInstances[0], observedInstances[1]);
            Assert.AreSame(observedInstances[0], observedInstances[2]);
        }

        [Test]
        public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithoutMiddlewares>(pipeline => pipeline.Use<TestCommandMiddleware2>());

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.That(exception?.Message, Contains.Substring("No service for type"));
            Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware2)));
            Assert.That(exception?.Message, Contains.Substring("has been registered"));
        }

        [Test]
        public void GivenPipelineWithoutResponseThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithoutResponseWithoutMiddlewares>(pipeline => pipeline.Use<TestCommandMiddleware2>());

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.That(exception?.Message, Contains.Substring("No service for type"));
            Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware2)));
            Assert.That(exception?.Message, Contains.Substring("has been registered"));
        }

        [Test]
        public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutMiddlewares>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithoutMiddlewares>(pipeline => pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.That(exception?.Message, Contains.Substring("No service for type"));
            Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware)));
            Assert.That(exception?.Message, Contains.Substring("has been registered"));
        }

        [Test]
        public void GivenPipelineWithoutResponseThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponseWithoutMiddlewares>()
                        .AddSingleton(observations);

            _ = services.ConfigureCommandPipeline<TestCommandHandlerWithoutResponseWithoutMiddlewares>(pipeline => pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.That(exception?.Message, Contains.Substring("No service for type"));
            Assert.That(exception?.Message, Contains.Substring(nameof(TestCommandMiddleware)));
            Assert.That(exception?.Message, Contains.Substring("has been registered"));
        }

        [Test]
        public void InvalidMiddlewares()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddScoped<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddSingleton<TestCommandMiddlewareWithMultipleInterfaces>().ConfigureConqueror());
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

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(command.Payload + 1);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new());
            }
        }

        private sealed class TestCommandHandlerWithSingleMiddlewareWithParameter : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithSingleMiddlewareWithParameter(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
                return new(command.Payload + 1);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 });
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithSingleMiddleware : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithSingleMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new());
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithSingleMiddlewareWithParameter(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new() { Parameter = 10 });
            }
        }

        private sealed class TestCommandHandlerWithMultipleMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithMultipleMiddlewares(TestObservations observations)
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

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                            .Use<TestCommandMiddleware2>();
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithMultipleMiddlewares : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithMultipleMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                            .Use<TestCommandMiddleware2>();
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

        private sealed class TestCommandHandlerWithRetryMiddleware : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithRetryMiddleware(TestObservations observations)
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

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<TestCommandRetryMiddleware>()
                            .Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new())
                            .Use<TestCommandMiddleware2>();
            }
        }

        private sealed class TestCommandHandlerWithMultipleMutatingMiddlewares : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithMultipleMutatingMiddlewares(TestObservations observations)
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

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<MutatingTestCommandMiddleware>()
                            .Use<MutatingTestCommandMiddleware2>();
            }
        }

        private sealed class TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponseWithMultipleMutatingMiddlewares(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.CommandsFromHandlers.Add(command);
                observations.CancellationTokensFromHandlers.Add(cancellationToken);
            }

            // ReSharper disable once UnusedMember.Local
            public static void ConfigurePipeline(ICommandPipelineBuilder pipeline)
            {
                _ = pipeline.Use<MutatingTestCommandMiddleware>()
                            .Use<MutatingTestCommandMiddleware2>();
            }
        }

        private sealed record TestCommandMiddlewareConfiguration
        {
            public int Parameter { get; set; }
        }

        private sealed class TestCommandMiddleware : ICommandMiddleware<TestCommandMiddlewareConfiguration>
        {
            private readonly TestObservations observations;

            public TestCommandMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandMiddleware2 : ICommandMiddleware
        {
            private readonly TestObservations observations;

            public TestCommandMiddleware2(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandRetryMiddleware : ICommandMiddleware
        {
            private readonly TestObservations observations;

            public TestCommandRetryMiddleware(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

                _ = await ctx.Next(ctx.Command, ctx.CancellationToken);
                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class MutatingTestCommandMiddleware : ICommandMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestCommandMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

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

        private sealed class MutatingTestCommandMiddleware2 : ICommandMiddleware
        {
            private readonly CancellationTokensToUse cancellationTokensToUse;
            private readonly TestObservations observations;

            public MutatingTestCommandMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
            {
                this.observations = observations;
                this.cancellationTokensToUse = cancellationTokensToUse;
            }

            public async Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class
            {
                await Task.Yield();
                observations.MiddlewareTypes.Add(GetType());
                observations.CommandsFromMiddlewares.Add(ctx.Command);
                observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

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

        private sealed class TestCommandMiddlewareWithMultipleInterfaces : ICommandMiddleware<TestCommandMiddlewareConfiguration>,
                                                                           ICommandMiddleware
        {
            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse> ctx)
                where TCommand : class =>
                throw new InvalidOperationException("this middleware should never be called");

            public Task<TResponse> Execute<TCommand, TResponse>(CommandMiddlewareContext<TCommand, TResponse, TestCommandMiddlewareConfiguration> ctx)
                where TCommand : class =>
                throw new InvalidOperationException("this middleware should never be called");
        }

        private sealed class TestObservations
        {
            public List<Type> MiddlewareTypes { get; } = new();

            public List<object> CommandsFromHandlers { get; } = new();

            public List<object> CommandsFromMiddlewares { get; } = new();

            public List<object> ResponsesFromMiddlewares { get; } = new();

            public List<CancellationToken> CancellationTokensFromHandlers { get; } = new();

            public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

            public List<object> ConfigurationFromMiddlewares { get; } = new();
        }

        private sealed class CancellationTokensToUse
        {
            public List<CancellationToken> CancellationTokens { get; } = new();
        }

        private sealed class TestService
        {
        }
    }
}
