namespace Conqueror.CQS.Tests.QueryHandling;

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

        var handler = provider.GetRequiredService<IGenericTestQueryHandler<string>>();

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

        var handler = provider.GetRequiredService<ITestQueryHandler>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(new(), tokenSource.Token);

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

        _ = await handler.Handle(new());

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

        var response = await handler.Handle(query, CancellationToken.None);

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

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10), CancellationToken.None));

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

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    public interface ITestQueryHandler2 : IQueryHandler<TestQuery2, TestQueryResponse2>;

    public interface IGenericTestQueryHandler<T> : IQueryHandler<GenericTestQuery<T>, GenericTestQueryResponse<T>>;

    public interface IThrowingQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    public interface ITestQueryHandlerWithExtraMethod : IQueryHandler<TestQuery, TestQueryResponse>
    {
        void ExtraMethod();
    }

    private sealed class TestQueryHandler(TestObservations observations) : ITestQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }
    }

    private sealed class TestQueryHandlerWithMultipleInterfaces(TestObservations observations) : ITestQueryHandler,
                                                                                                 ITestQueryHandler2,
                                                                                                 ICommandHandler<TestCommand, TestCommandResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }

        public async Task<TestQueryResponse2> Handle(TestQuery2 query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new();
        }

        public async Task<TestCommandResponse> Handle(TestCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Instances.Add(this);
            return new();
        }
    }

    private sealed class GenericTestQueryHandler<T>(TestObservations observations) : IGenericTestQueryHandler<T>
    {
        public async Task<GenericTestQueryResponse<T>> Handle(GenericTestQuery<T> query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload);
        }
    }

    private sealed class ThrowingQueryHandler(Exception exception) : IThrowingQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestQueryHandlerWithCustomInterfaceWithExtraMethod : ITestQueryHandlerWithExtraMethod
    {
        public Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public void ExtraMethod() => throw new NotSupportedException();
    }

    private sealed class TestObservations
    {
        public List<object> Instances { get; } = [];

        public List<object> Queries { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
