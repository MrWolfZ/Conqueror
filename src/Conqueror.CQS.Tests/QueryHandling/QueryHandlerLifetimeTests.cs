namespace Conqueror.CQS.Tests.QueryHandling;

public sealed class QueryHandlerLifetimeTests
{
    [Test]
    public async Task GivenTransientHandler_ResolvingHandlerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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
    public async Task GivenTransientHandlerWithFactory_ResolvingHandlerCreatesNewInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler(p => new TestQueryHandler(p.GetRequiredService<TestObservations>()))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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
    public async Task GivenScopedHandlerWithFactory_ResolvingHandlerCreatesNewInstanceForEveryScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler(p => new TestQueryHandler(p.GetRequiredService<TestObservations>()), ServiceLifetime.Scoped)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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
    public async Task GivenSingletonHandlerWithFactory_ResolvingHandlerReturnsSameInstanceEveryTime()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler(p => new TestQueryHandler(p.GetRequiredService<TestObservations>()), ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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

        _ = services.AddSingleton<TestQueryHandlerWithMultipleInterfaces>()
                    .AddConquerorQueryHandler<TestQueryHandlerWithMultipleInterfaces>(p => p.GetRequiredService<TestQueryHandlerWithMultipleInterfaces>(),
                                                                                      ServiceLifetime.Singleton)
                    .AddConquerorCommandHandler<TestQueryHandlerWithMultipleInterfaces>(p => p.GetRequiredService<TestQueryHandlerWithMultipleInterfaces>(),
                                                                                        ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

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

        _ = services.AddConquerorQueryHandler(new TestQueryHandler(observations));

        var provider = services.BuildServiceProvider();

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

    private sealed class TestQueryHandler(TestObservations observations) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        private int invocationCount;

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery command, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
            return new();
        }
    }

    private sealed class TestQueryHandlerWithMultipleInterfaces(TestObservations observations) : IQueryHandler<TestQuery, TestQueryResponse>,
                                                                                                 IQueryHandler<TestQuery2, TestQueryResponse2>,
                                                                                                 ICommandHandler<TestCommand, TestCommandResponse>
    {
        private int invocationCount;

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

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            invocationCount += 1;
            await Task.Yield();
            observations.InvocationCounts.Add(invocationCount);
            return new();
        }
    }

    private sealed class TestObservations
    {
        public List<int> InvocationCounts { get; } = [];
    }
}
