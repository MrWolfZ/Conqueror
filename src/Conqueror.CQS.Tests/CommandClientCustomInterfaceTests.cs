using Conqueror.CQS.Common;

namespace Conqueror.CQS.Tests
{
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
    public abstract class CommandClientCustomInterfaceTests
    {
        [Test]
        public async Task GivenCustomHandlerInterface_ClientCanBeCreated()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            AddCommandClient<ITestCommandHandler>(services, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

            _ = services.AddConquerorCQS()
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

            AddCommandClient<ITestCommandWithoutResponseHandler>(services, b => b.ServiceProvider.GetRequiredService<TestCommandTransport>());

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandTransport>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var client = provider.GetRequiredService<ITestCommandWithoutResponseHandler>();

            var command = new TestCommandWithoutResponse();

            await client.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        protected abstract void AddCommandClient<THandler>(IServiceCollection services,
                                                           Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                           Action<ICommandPipelineBuilder>? configurePipeline = null)
            where THandler : class, ICommandHandler;

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

    [TestFixture]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
    public sealed class CommandClientCustomInterfaceWithSyncFactoryTests : CommandClientCustomInterfaceTests
    {
        protected override void AddCommandClient<THandler>(IServiceCollection services,
                                                           Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                           Action<ICommandPipelineBuilder>? configurePipeline = null)
        {
            _ = services.AddConquerorCommandClient<THandler>(transportClientFactory, configurePipeline);
        }
    }

    [TestFixture]
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "it makes sense for these test sub-classes to be here")]
    public sealed class CommandClientCustomInterfaceWithAsyncFactoryTests : CommandClientCustomInterfaceTests
    {
        protected override void AddCommandClient<THandler>(IServiceCollection services,
                                                           Func<ICommandTransportClientBuilder, ICommandTransportClient> transportClientFactory,
                                                           Action<ICommandPipelineBuilder>? configurePipeline = null)
        {
            _ = services.AddConquerorCommandClient<THandler>(async b =>
                                                             {
                                                                 await Task.Delay(1);
                                                                 return transportClientFactory(b);
                                                             },
                                                             configurePipeline);
        }
    }
}
