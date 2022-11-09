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
        public async Task GivenGenericCommand_HandlerReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<GenericTestCommandHandler<string>>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<GenericTestCommand<string>, GenericTestCommandResponse<string>>>();

            var command = new GenericTestCommand<string>("test string");

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
        public async Task GivenGenericCommandWithoutResponse_HandlerReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<GenericTestCommandHandlerWithoutResponse<string>>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<GenericTestCommand<string>>>();

            var command = new GenericTestCommand<string>("test string");

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
        public void GivenExceptionInHandler_InvocationThrowsSameException()
        {
            var services = new ServiceCollection();
            var exception = new Exception();

            _ = services.AddConquerorCQS()
                        .AddTransient<ThrowingCommandHandler>()
                        .AddSingleton(exception);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.AreSame(exception, thrownException);
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

        private sealed record GenericTestCommand<T>(T Payload);

        private sealed record GenericTestCommandResponse<T>(T Payload);

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations responses;

            public TestCommandHandler(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
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

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                responses.Commands.Add(command);
                responses.CancellationTokens.Add(cancellationToken);
            }
        }

        private sealed class GenericTestCommandHandler<T> : ICommandHandler<GenericTestCommand<T>, GenericTestCommandResponse<T>>
        {
            private readonly TestObservations responses;

            public GenericTestCommandHandler(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task<GenericTestCommandResponse<T>> ExecuteCommand(GenericTestCommand<T> command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                responses.Commands.Add(command);
                responses.CancellationTokens.Add(cancellationToken);
                return new(command.Payload);
            }
        }
        
        private sealed class GenericTestCommandHandlerWithoutResponse<T> : ICommandHandler<GenericTestCommand<T>>
        {
            private readonly TestObservations responses;

            public GenericTestCommandHandlerWithoutResponse(TestObservations responses)
            {
                this.responses = responses;
            }

            public async Task ExecuteCommand(GenericTestCommand<T> command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                responses.Commands.Add(command);
                responses.CancellationTokens.Add(cancellationToken);
            }
        }

        private sealed class ThrowingCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly Exception exception;

            public ThrowingCommandHandler(Exception exception)
            {
                this.exception = exception;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                throw exception;
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
