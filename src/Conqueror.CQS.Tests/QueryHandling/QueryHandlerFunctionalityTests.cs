namespace Conqueror.CQS.Tests.QueryHandling;

public abstract class QueryHandlerFunctionalityTests
{
    protected abstract IServiceCollection RegisterHandler(IServiceCollection services);

    protected virtual IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
    }

    protected virtual TestQuery CreateQuery() => new(10);

    protected virtual TestQueryResponse CreateExpectedResponse() => new(11);

    [Test]
    public async Task GivenQuery_HandlerReceivesQuery()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var query = CreateQuery();

        _ = await handler.Handle(query);

        Assert.That(observations.Queries, Is.EquivalentTo(new[] { query }));
    }

    [Test]
    public async Task GivenCancellationToken_HandlerReceivesCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.Handle(CreateQuery(), tokenSource.Token);

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { tokenSource.Token }));
    }

    [Test]
    public async Task GivenNoCancellationToken_HandlerReceivesDefaultCancellationToken()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        _ = await handler.Handle(CreateQuery());

        Assert.That(observations.CancellationTokens, Is.EquivalentTo(new[] { default(CancellationToken) }));
    }

    [Test]
    public async Task GivenQuery_HandlerReturnsResponse()
    {
        var observations = new TestObservations();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var query = CreateQuery();

        var response = await handler.Handle(query);

        Assert.That(response, Is.EqualTo(CreateExpectedResponse()));
    }

    [Test]
    public void GivenExceptionInHandler_InvocationThrowsSameException()
    {
        var observations = new TestObservations();
        var exception = new Exception();

        var provider = RegisterHandler(new ServiceCollection())
                       .AddSingleton(observations)
                       .AddSingleton(exception)
                       .BuildServiceProvider();

        var handler = ResolveHandler(provider);

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(CreateQuery()));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    public record TestQuery(int Payload);

    public record TestQueryResponse(int Payload);

    protected sealed class TestObservations
    {
        public List<object> Queries { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityDefaultTests : QueryHandlerFunctionalityTests
{
    [Test]
    public async Task GivenDisposableHandler_WhenServiceProviderIsDisposed_ThenHandlerIsDisposed()
    {
        var services = new ServiceCollection();
        var observation = new DisposalObservation();

        _ = services.AddConquerorQueryHandler<DisposableQueryHandler>()
                    .AddSingleton(observation);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.Handle(new(10));

        await provider.DisposeAsync();

        Assert.That(observation.WasDisposed, Is.True);
    }

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorQueryHandler<TestQueryHandler>();
    }

    private sealed class TestQueryHandler(TestObservations observations, Exception? exception = null) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }
    }

    private sealed class DisposableQueryHandler(DisposalObservation observation) : IQueryHandler<TestQuery, TestQueryResponse>, IDisposable
    {
        public async Task<TestQueryResponse> Handle(TestQuery command, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(command.Payload);
        }

        public void Dispose() => observation.WasDisposed = true;
    }

    private sealed class DisposalObservation
    {
        public bool WasDisposed { get; set; }
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityDelegateTests : QueryHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (query, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Queries.Add(query);
            obs.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        });
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityGenericTests : QueryHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorQueryHandler<GenericTestQueryHandler<string>>();
    }

    protected override IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<GenericTestQuery<string>, GenericTestQueryResponse<string>>>();
        return new AdapterHandler<string>(handler);
    }

    protected override TestQuery CreateQuery() => new GenericTestQuery<string>("test");

    protected override TestQueryResponse CreateExpectedResponse() => new GenericTestQueryResponse<string>("test");

    private sealed record GenericTestQuery<T>(T GenericPayload) : TestQuery(10);

    private sealed record GenericTestQueryResponse<T>(T GenericPayload) : TestQueryResponse(11);

    private sealed class GenericTestQueryHandler<T>(TestObservations observations, Exception? exception = null) : IQueryHandler<GenericTestQuery<T>, GenericTestQueryResponse<T>>
    {
        public async Task<GenericTestQueryResponse<T>> Handle(GenericTestQuery<T> query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.GenericPayload);
        }
    }

    private sealed class AdapterHandler<T>(IQueryHandler<GenericTestQuery<T>, GenericTestQueryResponse<T>> wrapped) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return await wrapped.Handle((GenericTestQuery<T>)query, cancellationToken);
        }
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityCustomInterfaceTests : QueryHandlerFunctionalityTests
{
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

    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddConquerorQueryHandler<TestQueryHandler>();
    }

    protected override IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<ITestQueryHandler>();
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;

    private sealed class TestQueryHandler(TestObservations observations, Exception? exception = null) : ITestQueryHandler
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        }
    }
}

public abstract class QueryHandlerFunctionalityClientTests : QueryHandlerFunctionalityTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return services.AddSingleton<TestQueryTransport>();
    }

    protected sealed class TestQueryTransport(TestObservations observations, Exception? exception = null) : IQueryTransportClient
    {
        public string TransportTypeName => "test";

        public async Task<TResponse> Send<TQuery, TResponse>(TQuery query,
                                                             IServiceProvider serviceProvider,
                                                             CancellationToken cancellationToken)
            where TQuery : class
        {
            await Task.Yield();

            if (exception is not null)
            {
                throw exception;
            }

            observations.Queries.Add(query);
            observations.CancellationTokens.Add(cancellationToken);

            var cmd = (TestQuery)(object)query;
            return (TResponse)(object)new TestQueryResponse(cmd.Payload + 1);
        }
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientWithSyncTransportFactoryTests : QueryHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientCustomInterfaceWithSyncTransportFactoryTests : QueryHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorQueryClient<ITestQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientWithAsyncTransportFactoryTests : QueryHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async b =>
                   {
                       await Task.Delay(1);
                       return b.ServiceProvider.GetRequiredService<TestQueryTransport>();
                   });
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientCustomInterfaceWithAsyncTransportFactoryTests : QueryHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services)
                   .AddConquerorQueryClient<ITestQueryHandler>(async b =>
                   {
                       await Task.Delay(1);
                       return b.ServiceProvider.GetRequiredService<TestQueryTransport>();
                   });
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;
}

public abstract class QueryHandlerFunctionalityClientFromFactoryTests : QueryHandlerFunctionalityClientTests
{
    protected override IServiceCollection RegisterHandler(IServiceCollection services)
    {
        return base.RegisterHandler(services).AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (query, p, cancellationToken) =>
        {
            await Task.Yield();

            var exception = p.GetService<Exception>();
            if (exception is not null)
            {
                throw exception;
            }

            var obs = p.GetRequiredService<TestObservations>();
            obs.Queries.Add(query);
            obs.CancellationTokens.Add(cancellationToken);
            return new(query.Payload + 1);
        });
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientFromFactoryWithSyncTransportFactoryTests : QueryHandlerFunctionalityClientFromFactoryTests
{
    protected override IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IQueryClientFactory>()
                              .CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientWithCustomInterfaceFromFactoryWithSyncTransportFactoryTests : QueryHandlerFunctionalityClientFromFactoryTests
{
    protected override IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IQueryClientFactory>()
                              .CreateQueryClient<ITestQueryHandler>(b => b.ServiceProvider.GetRequiredService<TestQueryTransport>());
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientFromFactoryWithAsyncTransportFactoryTests : QueryHandlerFunctionalityClientFromFactoryTests
{
    protected override IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IQueryClientFactory>()
                              .CreateQueryClient<IQueryHandler<TestQuery, TestQueryResponse>>(async b =>
                              {
                                  await Task.Delay(1);
                                  return b.ServiceProvider.GetRequiredService<TestQueryTransport>();
                              });
    }
}

[TestFixture]
public sealed class QueryHandlerFunctionalityClientWithCustomInterfaceFromFactoryWithAsyncTransportFactoryTests : QueryHandlerFunctionalityClientFromFactoryTests
{
    protected override IQueryHandler<TestQuery, TestQueryResponse> ResolveHandler(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IQueryClientFactory>()
                              .CreateQueryClient<ITestQueryHandler>(async b =>
                              {
                                  await Task.Delay(1);
                                  return b.ServiceProvider.GetRequiredService<TestQueryTransport>();
                              });
    }

    public interface ITestQueryHandler : IQueryHandler<TestQuery, TestQueryResponse>;
}
