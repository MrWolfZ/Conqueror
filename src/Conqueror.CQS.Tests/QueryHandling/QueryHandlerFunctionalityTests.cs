namespace Conqueror.CQS.Tests.QueryHandling;

public sealed class QueryHandlerFunctionalityTests
{
    [Test]
    public async Task GivenQuery_HandlerReceivesQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.Handle(query, CancellationToken.None);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenQuery_DelegateHandlerReceivesQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (query, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Queries.Add(query);
                        obs.CancellationTokens.Add(cancellationToken);
                        return new(query.Payload + 1);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.Handle(query, CancellationToken.None);

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

        var handler = provider.GetRequiredService<IQueryHandler<GenericTestQuery<string>, GenericTestQueryResponse<string>>>();

        var query = new GenericTestQuery<string>("test string");

        _ = await handler.Handle(query, CancellationToken.None);

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

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenCancellationToken_DelegateHandlerReceivesCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (command, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Queries.Add(command);
                        obs.CancellationTokens.Add(cancellationToken);
                        return new(command.Payload + 1);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(new(10), tokenSource.Token);

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

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new(10));

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenNoCancellationToken_DelegateHandlerReceivesDefaultCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (command, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.Queries.Add(command);
                        obs.CancellationTokens.Add(cancellationToken);
                        return new(command.Payload + 1);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new(10));

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

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        var response = await handler.Handle(query, CancellationToken.None);

        Assert.That(response.Payload, Is.EqualTo(query.Payload + 1));
    }

    [Test]
    public async Task GivenQuery_DelegateHandlerReturnsResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (command, _, _) =>
                    {
                        await Task.Yield();
                        return new(command.Payload + 1);
                    })
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var command = new TestQuery(10);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.That(response.Payload, Is.EqualTo(command.Payload + 1));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorQueryHandler<ThrowingQueryHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10), CancellationToken.None));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<DisposableQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new(10), CancellationToken.None);

        await provider.DisposeAsync();

        Assert.That(observations.DisposedTypes, Is.EquivalentTo(new[] { typeof(DisposableQueryHandler) }));
    }

    [Test]
    public void GivenHandlerWithInvalidInterface_RegisteringHandlerThrowsArgumentException()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithoutValidInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryHandler<TestQueryHandlerWithoutValidInterfaces>(_ => new()));
    }

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int Payload);

    private sealed record GenericTestQuery<T>(T Payload);

    private sealed record GenericTestQueryResponse<T>(T Payload);

    private sealed class TestQueryHandler(TestObservations observations) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }
    }

    private sealed class GenericTestQueryHandler<T>(TestObservations observations) : IQueryHandler<GenericTestQuery<T>, GenericTestQueryResponse<T>>
    {
        public async Task<GenericTestQueryResponse<T>> Handle(GenericTestQuery<T> query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload);
        }
    }

    private sealed class ThrowingQueryHandler(Exception exception) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class DisposableQueryHandler(TestObservations observations) : IQueryHandler<TestQuery, TestQueryResponse>, IDisposable
    {
        public async Task<TestQueryResponse> Handle(TestQuery command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(command.Payload);
        }

        public void Dispose()
        {
            observations.DisposedTypes.Add(GetType());
        }
    }

    private sealed class TestQueryHandlerWithoutValidInterfaces : IQueryHandler;

    private sealed class TestObservations
    {
        public List<object> Queries { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public List<Type> DisposedTypes { get; } = [];
    }
}
