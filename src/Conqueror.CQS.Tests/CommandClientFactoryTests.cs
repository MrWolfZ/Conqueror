using Conqueror.CQS.CommandHandling;
using Conqueror.CQS.Common;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandClientFactoryTests
    {
        [Test]
        public async Task GivenPlainHandlerInterface_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            var client = clientFactory.CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

            var command = new TestCommand();

            _ = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            var client = clientFactory.CreateCommandClient<ITestCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

            var command = new TestCommand();

            _ = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenPlainHandlerInterfaceWithoutResponse_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            var client = clientFactory.CreateCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

            var command = new TestCommandWithoutResponse();

            await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenCustomHandlerInterfaceWithoutResponse_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            var client = clientFactory.CreateCommandClient<ITestCommandWithoutResponseHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

            var command = new TestCommandWithoutResponse();

            await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenPlainClientWithPipeline_PipelineIsCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            var client = clientFactory.CreateCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>(),
                                                                                                              p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

            var command = new TestCommand();

            _ = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
        }

        [Test]
        public async Task GivenCustomClientWithPipeline_PipelineIsCalled()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddTransient<TestCommandMiddleware>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            var client = clientFactory.CreateCommandClient<ITestCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>(),
                                                                                p => p.Use<TestCommandMiddleware, TestCommandMiddlewareConfiguration>(new()));

            var command = new TestCommand();

            _ = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestCommandMiddleware) }));
        }

        [Test]
        public void GivenCustomerHandlerInterfaceWithExtraMethods_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateCommandClient<ITestCommandHandlerWithExtraMethod>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
        }
        
        [Test]
        public void GivenNonGenericCommandHandlerInterface_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateCommandClient<INonGenericCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
        }
        
        [Test]
        public void GivenConcreteCommandHandlerType_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateCommandClient<TestCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
        }
        
        [Test]
        public void GivenCommandHandlerInterfaceThatImplementsMultipleOtherPlainCommandHandlerInterfaces_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateCommandClient<ICombinedCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
        }
        
        [Test]
        public void GivenCommandHandlerInterfaceThatImplementsMultipleOtherCustomCommandHandlerInterfaces_CreatingClientThrowsArgumentException()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var clientFactory = provider.GetRequiredService<ICommandClientFactory>();

            _ = Assert.Throws<ArgumentException>(() => clientFactory.CreateCommandClient<ICombinedCustomCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>()));
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        public sealed record TestCommandWithoutResponse;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        public interface ITestCommandWithoutResponseHandler : ICommandHandler<TestCommandWithoutResponse>
        {
        }

        public interface ITestCommandHandlerWithExtraMethod : ICommandHandler<TestCommand, TestCommandResponse>
        {
            void ExtraMethod();
        }

        public interface ICombinedCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>, ICommandHandler<TestCommandWithoutResponse>
        {
        }

        public interface ICombinedCustomCommandHandler : ITestCommandHandler, ITestCommandWithoutResponseHandler
        {
        }

        public interface INonGenericCommandHandler : ICommandHandler
        {
            void SomeMethod();
        }
        
        private sealed class TestCommandHandler : ITestCommandHandler
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken) => throw new NotSupportedException();
        }

        private sealed record TestCommandMiddlewareConfiguration;

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

                return await ctx.Next(ctx.Command, ctx.CancellationToken);
            }
        }

        private sealed class TestCommandTransport : ICommandTransportClient
        {
            private readonly TestObservations responses;

            public TestCommandTransport(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                await Task.Yield();
                responses.Commands.Add(command);

                if (typeof(TResponse) == typeof(UnitCommandResponse))
                {
                    return (TResponse)(object)UnitCommandResponse.Instance;
                }

                return (TResponse)(object)new TestCommandResponse();
            }
        }

        private sealed class TestObservations
        {
            public List<object> Commands { get; } = new();

            public List<Type> MiddlewareTypes { get; } = new();
        }
    }
}
