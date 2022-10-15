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
        public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

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

            _ = Assert.Throws<ArgumentException>(() => services.AddConquerorCQS().AddTransient<TestCommandHandlerWithCustomInterfaceWithExtraMethod>().ConfigureConqueror());
        }

// interface and event types must be public for dynamic type generation to work
#pragma warning disable CA1034

        public sealed record TestCommand;

        public sealed record TestCommandResponse;

        public sealed record TestCommand2;

        public sealed record TestCommandResponse2;

        public sealed record TestCommandWithoutResponse;

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

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }

            public async Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken)
            {
                await Task.Yield();
                observations.Instances.Add(this);
                return new();
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
