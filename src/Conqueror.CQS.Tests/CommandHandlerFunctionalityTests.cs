namespace Conqueror.CQS.Tests
{
    public sealed class CommandHandlerFunctionalityTests
    {
        [Test]
        public async Task GivenCommand_HandlerReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            _ = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }
        
        [Test]
        public async Task GivenCommandWithoutResponse_HandlerReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            var command = new TestCommandWithoutResponse(10);

            await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.That(observations.Commands, Is.EquivalentTo(new[] { command }));
        }

        [Test]
        public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            using var tokenSource = new CancellationTokenSource();

            _ = await handler.ExecuteCommand(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }
        
        [Test]
        public async Task GivenCancellationTokenForHandlerWithoutResponse_HandlerReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            using var tokenSource = new CancellationTokenSource();

            await handler.ExecuteCommand(new(10), tokenSource.Token);

            Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
        }
        
        [Test]
        public async Task GivenCommand_HandlerReturnsResponse()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var command = new TestCommand(10);

            var response = await handler.ExecuteCommand(command, CancellationToken.None);

            Assert.AreEqual(command.Payload + 1, response.Payload);
        }

        [Test]
        public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
        {
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCQS().AddTransient<TestCommandHandlerWithoutValidInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCQS().AddScoped<TestCommandHandlerWithoutValidInterfaces>().ConfigureConqueror());
            _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorCQS().AddSingleton<TestCommandHandlerWithoutValidInterfaces>().ConfigureConqueror());
        }

        private sealed record TestCommand(int Payload);

        private sealed record TestCommandResponse(int Payload);

        private sealed record TestCommandWithoutResponse(int Payload);

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations responses;

            public TestCommandHandler(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                responses.Commands.Add(command);
                responses.CancellationTokens.Add(cancellationToken);
                return new(command.Payload + 1);
            }
        }
        
        private sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations responses;

            public TestCommandHandlerWithoutResponse(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                responses.Commands.Add(command);
                responses.CancellationTokens.Add(cancellationToken);
            }
        }

        private sealed class TestCommandHandlerWithoutValidInterfaces : ICommandHandler
        {
        }

        private sealed class TestObservations
        {
            public List<object> Commands { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
