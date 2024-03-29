namespace Conqueror.CQS.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "interface and event types must be public for dynamic type generation to work")]
public sealed class QueryHandlerCustomInterfaceTests
{
    [Test]
    public async Task GivenQuery_HandlerReceivesQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestQueryHandler>();

        var query = new TestQuery();

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenGenericQuery_HandlerReceivesQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<GenericTestQueryHandler<string>>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IGenericTestQueryHandler<string>>();

        var query = new GenericTestQuery<string>("test string");

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestQueryHandler>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteQuery(new(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestQueryHandler>();

        _ = await handler.ExecuteQuery(new());

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenQuery_HandlerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<ITestQueryHandler>();

        var query = new TestQuery(10);

        var response = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(response.Payload, Is.EqualTo(query.Payload + 1));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorQueryHandler<ThrowingQueryHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IThrowingQueryHandler>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromPlainInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
    }

    [Test]
    public void GivenHandlerWithCustomInterface_HandlerCanBeResolvedFromCustomInterface()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
    }

    [Test]
    public async Task GivenHandlerWithCustomInterface_ResolvingHandlerViaPlainAndCustomInterfaceReturnsEquivalentInstance()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>(ServiceLifetime.Singleton)
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var plainInterfaceHandler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var customInterfaceHandler = provider.GetRequiredService<ITestQueryHandler>();

        _ = await plainInterfaceHandler.ExecuteQuery(new(), CancellationToken.None);
        _ = await customInterfaceHandler.ExecuteQuery(new(), CancellationToken.None);

        Assert.That(observations.Instances, Has.Count.EqualTo(2));
        Assert.That(observations.Instances[1], Is.SameAs(observations.Instances[0]));
    }

    [Test]
    public void GivenHandlerWithMultipleCustomInterfaces_HandlerCanBeResolvedFromAllInterfaces()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleInterfaces>()
                    .AddConquerorCommandHandler<TestQueryHandlerWithMultipleInterfaces>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<IQueryHandler<TestQuery2, TestQueryResponse2>>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ITestQueryHandler2>());
        Assert.DoesNotThrow(() => provider.GetRequiredService<ICommandHandler<TestCommand, TestCommandResponse>>());
    }

    [Test]
    public void GivenHandlerWithCustomInterfaceWithExtraMethods_RegisteringHandlerThrowsArgumentException()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentException>(() => services.AddConquerorQueryHandler<TestQueryHandlerWithCustomInterfaceWithExtraMethod>());
    }

    public sealed record TestQuery(int Payload = 0);

    public sealed record TestQueryResponse(int Payload);

    public sealed record TestQuery2;

    public sealed record TestQueryResponse2;

    public sealed record GenericTestQuery<T>(T Payload);

    public sealed record GenericTestQueryResponse<T>(T Payload);

    public sealed record TestCommand;

    public sealed record TestCommandResponse;

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }

    public interface ITestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>
    {
    }

    public interface IGenericTestQueryHandler<T> : IQueryHandler<GenericTestQuery<T>, GenericTestQueryResponse<T>>
    {
    }

    public interface IThrowingQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>
    {
    }

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }

    private sealed class TestQueryHandler : ITestQueryHandler
    {
        private readonly TestObservations observations;

        public TestQueryHandler(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }
    }

    private sealed class TestQueryHandlerWithMultipleInterfaces : ITestQueryHandler,
                                                                  ITestQueryHandler2,
                                                                  ICommandHandler<TestCommand, TestCommandResponse>
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithMultipleInterfaces(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }

        public async Task<TestQueryResponse2> ExecuteQuery(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new();
        }

        public async Task<TestCommandResponse> ExecuteCommand(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            return new();
        }
    }

    private sealed class GenericTestQueryHandler<T> : IGenericTestQueryHandler<T>
    {
        private readonly TestObservations observations;

        public GenericTestQueryHandler(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<GenericTestQueryResponse<T>> ExecuteQuery(GenericTestQuery<T> query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload);
        }
    }

    private sealed class ThrowingQueryHandler : IThrowingQueryHandler
    {
        private readonly Exception exception;

        public ThrowingQueryHandler(Exception exception)
        {
            this.exception = exception;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestQueryHandlerWithCustomInterfaceWithExtraMethod : ITestQueryHandlerWithExtraMethod
    {
        public Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = new();

        public List<object> Queries { get; } = new();

        public List<CancellationToken> CancellationTokens { get; } = new();
    }
}
