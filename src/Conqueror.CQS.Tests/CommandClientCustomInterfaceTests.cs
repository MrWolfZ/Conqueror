using Conqueror.CQS.Common;

namespace Conqueror.CQS.Tests
{
    public sealed class CommandClientCustomInterfaceTests
    {
        [Test]
        public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ITestCommandHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandHandler>();

            var command = new TestCommand();

            _ = await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenCustomHandlerInterfaceWithoutResponse_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddConquerorCommandClient<ITestCommandWithoutResponseHandler>(b => b.ServiceProvider.GetRequiredService<TestCommandTransport>())
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandWithoutResponseHandler>();

            var command = new TestCommandWithoutResponse();

            await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
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
        }
    }
}
