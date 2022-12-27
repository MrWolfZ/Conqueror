namespace Conqueror.CQS.Tests
{
    public sealed class CommandHandlerLifetimeTests
    {
        [Test]
        public async Task GivenTransientHandler_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public async Task GivenTransientHandlerWithoutResponse_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler1.ExecuteCommand(new(), CancellationToken.None);
            await handler2.ExecuteCommand(new(), CancellationToken.None);
            await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedHandler_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddScoped<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public async Task GivenScopedHandlerWithoutResponse_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddScoped<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler1.ExecuteCommand(new(), CancellationToken.None);
            await handler2.ExecuteCommand(new(), CancellationToken.None);
            await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonHandlerWithoutResponse_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestCommandHandlerWithoutResponse>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();

            await handler1.ExecuteCommand(new(), CancellationToken.None);
            await handler2.ExecuteCommand(new(), CancellationToken.None);
            await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonHandlerWithMultipleHandlerInterfaces_ResolvingHandlerViaEitherInterfaceReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestCommandHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler1 = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = provider.GetRequiredService<ICommandHandler<TestCommand2, TestCommandResponse2>>();
            var handler3 = provider.GetRequiredService<ICommandHandler<TestCommandWithoutResponse>>();
            var handler4 = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            await handler3.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler4.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerViaConcreteClassReturnsSameInstanceAsResolvingViaInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestCommandHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler1 = provider.GetRequiredService<TestCommandHandler>();
            var handler2 = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonHandlerInstance_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton(new TestCommandHandler(observations));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler2.ExecuteCommand(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private sealed record TestCommand2;

        private sealed record TestCommandResponse2;

        private sealed record TestCommandWithoutResponse;

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed class TestCommandHandler : ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }
        }

        private sealed class TestCommandHandlerWithoutResponse : ICommandHandler<TestCommandWithoutResponse>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandHandlerWithoutResponse(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
            }
        }

        private sealed class TestCommandHandlerWithMultipleInterfaces : ICommandHandler<TestCommand, TestCommandResponse>, 
                                                                        ICommandHandler<TestCommand2, TestCommandResponse2>,
                                                                        ICommandHandler<TestCommandWithoutResponse>,
                                                                        IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestCommandHandlerWithMultipleInterfaces(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }

            public async Task<TestCommandResponse2> ExecuteCommand(TestCommand2 command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }

            public async Task ExecuteCommand(TestCommandWithoutResponse command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }
        }

        private sealed class TestObservations
        {
            public List<int> InvocationCounts { get; } = new();
        }
    }
}
