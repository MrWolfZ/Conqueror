namespace Conqueror.CQS.Tests
{
    public sealed class QueryHandlerLifetimeTests
    {
        [Test]
        public async Task GivenTransientHandler_ResolvingHandlerCreatesNewInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddTransient<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 1, 1 }));
        }

        [Test]
        public async Task GivenScopedHandler_ResolvingHandlerCreatesNewInstanceForEveryScope()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddScoped<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 1 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonHandlerWithMultipleHandlerInterfaces_ResolvingHandlerViaEitherInterfaceReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestQueryHandlerWithMultipleInterfaces>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler1 = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = provider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>();
            var handler3 = provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteCommand(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GivenSingletonHandler_ResolvingHandlerViaConcreteClassReturnsSameInstanceAsResolvingViaInterface()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton<TestQueryHandler>()
                        .AddSingleton(observations);

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            var handler1 = provider.GetRequiredService<TestQueryHandler>();
            var handler2 = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task GivenSingletonHandlerInstance_ResolvingHandlerReturnsSameInstanceEveryTime()
        {
            var services = new ServiceCollection();
            var observations = new TestObservations();

            _ = services.AddConquerorCQS()
                        .AddSingleton(new TestQueryHandler(observations));

            var provider = services.ConfigureConqueror().BuildServiceProvider();

            using var scope1 = provider.CreateScope();
            using var scope2 = provider.CreateScope();

            var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler2 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
            var handler3 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

            _ = await handler1.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler2.ExecuteQuery(new(), CancellationToken.None);
            _ = await handler3.ExecuteQuery(new(), CancellationToken.None);

            Assert.That(observations.InvocationCounts, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        private sealed record TestQuery;

        private sealed record TestQueryResponse;

        private sealed record TestQuery2;

        private sealed record TestQueryResponse2;

        private sealed record TestCommand;

        private sealed record TestCommandResponse;

        private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryHandler(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }
        }

        private sealed class TestQueryHandlerWithMultipleInterfaces : IQueryHandler<TestQuery, TestQueryResponse>,
                                                                      IQueryHandler<TestQuery2, TestQueryResponse2>,
                                                                      ICommandHandler<TestCommand, TestCommandResponse>
        {
            private readonly TestObservations observations;
            private int invocationCount;

            public TestQueryHandlerWithMultipleInterfaces(TestObservations observations)
            {
                this.observations = observations;
            }

            public async Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }

            public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 command, CancellationToken cancellationToken = default)
            {
                invocationCount += 1;
                await Task.Yield();
                observations.InvocationCounts.Add(invocationCount);
                return new();
            }

            public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken)
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
