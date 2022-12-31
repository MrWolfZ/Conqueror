namespace Conqueror.CQS.Tests
{
    public sealed class CommandHandlerCustomInterfaceTests
    {
        [Test]
        public async Task GivenCommand_HandlerReceivesCommand()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ITestCommandHandler>();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IGenericTestCommandHandler<string>>();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IGenericTestCommandHandlerWithoutResponse<string>>();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ITestCommandHandler>();
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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ITestCommandHandlerWithoutResponse>();
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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<ITestCommandHandler>();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var handler = provider.GetRequiredService<IThrowingTestCommandHandler>();

            var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteCommand(new(10), CancellationToken.None));

            Assert.AreSame(exception, thrownException);
        }

        [Test]
        public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

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

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            var plainInterfaceHandler = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var customInterfaceHandler = provider.GetRequiredService<ITestCommandHandlerWithoutResponse>();

            await plainInterfaceHandler.ExecuteCommand(new(), CancellationToken.None);
            await customInterfaceHandler.ExecuteCommand(new(), CancellationToken.None);

            Assert.AreEqual(2, observations.Instances.Count);
            Assert.AreSame(observations.Instances[0], observations.Instances[1]);
        }

        [Test]
        public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.FinalizeConquerorRegistrations().BuildServiceProvider();

            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandler2>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<ITestCommandHandlerWithoutResponse>());
            Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        }

        [Test]
        public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
        {
            var services = new ServiceCollection();

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().FinalizeConquerorRegistrations());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestCommand(int Payload = 0);

        public sealed record TestCommandResponse(int Payload);

        public sealed record TestCommand2;

        public sealed record TestCommandResponse2;

        public sealed record TestCommandWithoutResponse(int Payload = 0);

        public sealed record GenericTestCommand<T>(T Payload);

        public sealed record GenericTestCommandResponse<T>(T Payload);

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        public interface ITestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
        }

        public interface ITestCommandHandler2 : ICommandHandler<TestCommand2, TestCommandResponse2>
        {
        }

        public interface ITestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
        }

        public interface IGenericTestCommandHandler<T> : ICommandHandler<GenericTestCommand<T>, GenericTestCommandResponse<T>>
        {
        }

        public interface IGenericTestCommandHandlerWithoutResponse<T> : ICommandHandler<GenericTestCommand<T>>
        {
        }

        public interface IThrowingTestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
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

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                observations.Commands.Add(command);
                observations.CancellationTokens.Add(cancellationToken);
                return new(command.Payload + 1);
            }
        }

        private sealed class TestCommandHandlerWithoutResponse : ITestCommandHandlerWithoutResponse
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithoutResponse(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                observations.Commands.Add(command);
                observations.CancellationTokens.Add(cancellationToken);
            }
        }

        private sealed class TestCommandHandlerWithMultipleInterfaces : ITestCommandHandler,
                                                                        ITestCommandHandler2,
                                                                        ITestCommandHandlerWithoutResponse,
                                                                        IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;

            public TestCommandHandlerWithMultipleInterfaces(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                observations.Commands.Add(command);
                observations.CancellationTokens.Add(cancellationToken);
                return new(command.Payload + 1);
            }

            public async Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                observations.Commands.Add(command);
                observations.CancellationTokens.Add(cancellationToken);
                return new();
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Instances.Add(this);
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }
        }

        private sealed class GenericTestCommandHandler<T> : IGenericTestCommandHandler<T>
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

        private sealed class GenericTestCommandHandlerWithoutResponse<T> : IGenericTestCommandHandlerWithoutResponse<T>
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

        private sealed class ThrowingCommandHandler : IThrowingTestCommandHandler
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

        private sealed class TestCommandHandlerWithCustomInterfaceWithExtraMethod : ITestCommandHandlerWithExtraMethod
        {
            public Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public void ExtraMethod() => throw new NotSupportedException();
        }

        private sealed class TestObservations
        {
            public List<object> Instances { get; } = new();

            public List<object> Commands { get; } = new();

            public List<CancellationToken> CancellationTokens { get; } = new();
        }
    }
}
