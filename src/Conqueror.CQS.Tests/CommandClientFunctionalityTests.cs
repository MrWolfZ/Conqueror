using Conqueror.CQS.Common;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandClientFunctionalityTests
    {
        [Test]
        public async Task GivenCommand_TransportReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenCommandWithoutResponse_TransportReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var command = new TestCommandWithoutResponse(10);

            await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenCancellationToken_TransportReceivesCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            using var tokenSource = new CancellationTokenSource();

            _ = await client.ExecuteCommand(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenCancellationTokenForHandlerWithoutResponse_TransportReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommandWithoutResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            using var tokenSource = new CancellationTokenSource();

            await client.ExecuteCommand(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }

        [Test]
        public async Task GivenCommand_TransportReturnsResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            var response = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.AreEqual(command.Payload + 1, response.Payload);
        }

        [Test]
        public async Task GivenScopedFactory_TransportIsResolvedOnSameScope()
        {
            var seenInstances = new List<TestCommandTransport>();

            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b =>
                        {
                            var transport = b.ServiceProvider.GetRequiredService<TestCommandTransport>();
                            seenInstances.Add(transport);
                            return transport;
                        })
                        .AddScoped<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var client1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var client2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var client3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await client1.ExecuteCommand(new(10), CancellationToken.None);
            _ = await client2.ExecuteCommand(new(10), CancellationToken.None);
            _ = await client3.ExecuteCommand(new(10), CancellationToken.None);

            Assert.That(seenInstances, Has.Count.EqualTo(3));
            Assert.AreSame(seenInstances[0], seenInstances[1]);
            Assert.AreNotSame(seenInstances[0], seenInstances[2]);
        }

        [Test]
        public void GivenExceptionInTransport_InvocationThrowsSameException()
        {
            var services = new ServiceCollection();
            var exception = new Exception();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ICommandHandler<TestCommand, TestCommandResponse>>(b => b.ServiceProvider.GetRequiredService<ThrowingTestCommandTransport>())
                        .AddTransient<ThrowingTestCommandTransport>()
                        .AddSingleton(exception);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.AreSame(exception, thrownException);
        }

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int Payload);

        private sealed record TestCommandWithoutResponse(int Payload);

        private sealed class TestCommandTransport : ICommandTransportClient
        {
            private readonly TestObservations observations;

            public TestCommandTransport(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                await Task.Yield();
                observations.Commands.Add(command);
                observations.CancellationTokens.Add(cancellationToken);

                if (typeof(TResponse) == typeof(UnitCommandResponse))
                {
                    return (TResponse)(object)UnitCommandResponse.Instance;
                }

                var cmd = (TestCommand)(object)command;
                return (TResponse)(object)new TestCommandResponse(cmd.Payload + 1);
            }
        }

        private sealed class ThrowingTestCommandTransport : ICommandTransportClient
        {
            private readonly Exception exception;

            public ThrowingTestCommandTransport(Exception exception)
            {
                this.exception = exception;
            }

            public async Task<TResponse> ExecuteCommand<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
                where TCommand : class
            {
                await Task.Yield();
                throw exception;
            }
        }

        private sealed class TestObservations
        {
            public List<object> Commands { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
