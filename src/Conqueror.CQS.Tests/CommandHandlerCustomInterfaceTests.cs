using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandHandlerCustomInterfaceTests
    {
        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
        }
        
        [Test]
        public void GivenHandlerWithoutResponseWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
        }

        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
        }
        
        [Test]
        public void GivenHandlerWithoutResponseWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandlerWithoutResponse>());
        }

        [Test]
        public async Task GivenHandlerWithCustomInterface_ResolvingHandlerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var plainInterfaceHandler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var customInterfaceHandler = provider.GetRequiredService<ITestCommandHandler>();

            _ = await plainInterfaceHandler.ExecuteCommand(new(), CancellationToken.None);
            _ = await customInterfaceHandler.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreEqual(2, observations.Instances.Count);
            Assert.AreSame(observations.Instances[0], observations.Instances[1]);
        }

        [Test]
        public async Task GivenHandlerWithoutResponseWithCustomInterface_ResolvingHandlerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var plainInterfaceHandler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var customInterfaceHandler = provider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

            await plainInterfaceHandler.ExecuteCommand(new(), CancellationToken.None);
            await customInterfaceHandler.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreEqual(2, observations.Instances.Count);
            Assert.AreSame(observations.Instances[0], observations.Instances[1]);
        }

        [Test]
        public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestCommand;
        
        public sealed record TestCommandResponse;

        public sealed record TestCommandWithoutResponse;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
        }

        public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
        {
            void ExtraMethod();
        }

        private sealed class TestCommandHandler : ITestCommandHandler
        {
            private readonly TestObservations observations;

            public TestCommandHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }
        }

        private sealed class TestCommandHandlerWithoutResponse : ITestCommandHandlerWithoutResponse
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponse(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
            }
        }

        private sealed class TestCommandHandlerWithCustomInterfaceWithExtraMethod : ITestCommandHandlerWithExtraMethod
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();

            public void ExtraMethod() => throw new NotSupportedException();
        }

        private sealed class TestObservations
        {
            public List<object> Instances { get; } = new();
        }
    }
}
