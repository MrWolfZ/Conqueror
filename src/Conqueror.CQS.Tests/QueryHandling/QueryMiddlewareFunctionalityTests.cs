using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests.QueryHandling;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "types must be public to test case class")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "member order makes sense")]
public sealed class QueryMiddlewareFunctionalityTests
{
    [Test]
    [TestCaseSource(nameof(GenerateTestCases))]
    public async Task GivenClientAndHandlerPipelines_WhenHandlerIsCalled_MiddlewaresAreCalledWithQuery(ConquerorMiddlewareFunctionalityTestCase testCase)
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline => testCase.ConfigureHandlerPipeline?.Invoke(pipeline));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        using var tokenSource = new CancellationTokenSource();

        var query = new TestQuery(10);

        _ = await handler.WithPipeline(pipeline => testCase.ConfigureClientPipeline?.Invoke(pipeline)).ExecuteQuery(query, tokenSource.Token);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(query, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => new QueryTransportType(InMemoryQueryTransportTypeExtensions.TransportName, t.TransportRole))));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase> GenerateTestCases()
    {
        // no middleware
        yield return new(null,
                         null,
                         []);

        // single middleware
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Server),
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2>(),
                         null,
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2>(),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2>(),
                         null,
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2>(),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware>(),
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware>()
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware>()
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestQueryRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryRetryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryRetryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestQueryRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestQueryRetryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryRetryMiddleware), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                             (typeof(TestQueryRetryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2), QueryTransportRole.Client),
                             (typeof(TestQueryRetryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware), QueryTransportRole.Server),
                         ]);
    }

    [Test]
    public async Task GivenHandlerPipelineWithMutatingMiddlewares_WhenHandlerIsCalled_MiddlewaresCanChangeTheQueryAndResponseAndCancellationToken()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens)
                    .AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline =>
                    {
                        var obs = pipeline.ServiceProvider.GetRequiredService<TestObservations>();
                        var cancellationTokensToUse = pipeline.ServiceProvider.GetRequiredService<CancellationTokensToUse>();
                        _ = pipeline.Use(new MutatingTestQueryMiddleware(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestQueryMiddleware2(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var response = await handler.ExecuteQuery(new(0), tokens.CancellationTokens[0]);

        var query1 = new TestQuery(0);
        var query2 = new TestQuery(1);
        var query3 = new TestQuery(3);

        var response1 = new TestQueryResponse(0);
        var response2 = new TestQueryResponse(1);
        var response3 = new TestQueryResponse(3);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query1, query2 }));
        Assert.That(observations.QueriesFromHandlers, Is.EquivalentTo(new[] { query3 }));

        Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
        Assert.That(response, Is.EqualTo(response3));

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public void GivenHandlerPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(exception)
                    .AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline => pipeline.Use(new ThrowingTestQueryMiddleware(exception)));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteQuery(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public void GivenClientPipelineWithMiddlewareThatThrows_WhenHandlerIsCalled_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestQueryMiddleware(exception))).ExecuteQuery(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenMultipleClientPipelineConfigurations_WhenHandlerIsCalled_PipelinesAreExecutedInReverseOrder()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.WithPipeline(p => p.Use(new TestQueryMiddleware(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .WithPipeline(p => p.Use(new TestQueryMiddleware2(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .ExecuteQuery(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (command, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.QueriesFromHandlers.Add(command);
                        obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        return new(command.Payload + 1);
                    }, pipeline => pipeline.Use(new TestQueryMiddleware(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var command = new TestQuery(10);

        _ = await handler.ExecuteQuery(command);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase(
        Action<IQueryPipeline<TestQuery, TestQueryResponse>>? ConfigureHandlerPipeline,
        Action<IQueryPipeline<TestQuery, TestQueryResponse>>? ConfigureClientPipeline,
        IReadOnlyCollection<(Type MiddlewareType, QueryTransportRole TransportRole)> ExpectedMiddlewareTypes);

    public sealed record TestQuery(int Payload);

    public sealed record TestQueryResponse(int Payload);

    private sealed class TestQueryHandler(TestObservations observations) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(IQueryPipeline<TestQuery, TestQueryResponse> pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestQueryMiddleware(TestObservations observations) : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryMiddleware2(TestObservations observations) : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryRetryMiddleware(TestObservations observations) : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            _ = await ctx.Next(ctx.Query, ctx.CancellationToken);
            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestQueryMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var query = ctx.Query;

            if (query is TestQuery testQuery)
            {
                query = (TQuery)(object)new TestQuery(testQuery.Payload + 1);
            }

            var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[1]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestQueryResponse testQueryResponse)
            {
                response = (TResponse)(object)new TestQueryResponse(testQueryResponse.Payload + 2);
            }

            return response;
        }
    }

    private sealed class MutatingTestQueryMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            var query = ctx.Query;

            if (query is TestQuery testQuery)
            {
                query = (TQuery)(object)new TestQuery(testQuery.Payload + 2);
            }

            var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[2]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestQueryResponse testQueryResponse)
            {
                response = (TResponse)(object)new TestQueryResponse(testQueryResponse.Payload + 1);
            }

            return response;
        }
    }

    private sealed class ThrowingTestQueryMiddleware(Exception exception) : IQueryMiddleware
    {
        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = [];

        public List<object> QueriesFromHandlers { get; } = [];

        public List<object> QueriesFromMiddlewares { get; } = [];

        public List<object> ResponsesFromMiddlewares { get; } = [];

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = [];

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = [];

        public List<QueryTransportType> TransportTypesFromMiddlewares { get; } = [];
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = [];
    }
}
