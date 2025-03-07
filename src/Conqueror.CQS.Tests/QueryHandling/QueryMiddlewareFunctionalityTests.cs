using Conqueror.CQS.QueryHandling;

namespace Conqueror.CQS.Tests.QueryHandling;

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

        _ = await handler.WithPipeline(pipeline => testCase.ConfigureClientPipeline?.Invoke(pipeline)).Handle(query, tokenSource.Token);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(query, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(Enumerable.Repeat(tokenSource.Token, testCase.ExpectedMiddlewareTypes.Count)));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => t.MiddlewareType)));
        Assert.That(observations.TransportTypesFromMiddlewares, Is.EquivalentTo(testCase.ExpectedMiddlewareTypes.Select(t => new QueryTransportType(InProcessQueryTransportTypeExtensions.TransportName, t.TransportRole))));
    }

    private static IEnumerable<ConquerorMiddlewareFunctionalityTestCase> GenerateTestCases()
    {
        // no middleware
        yield return new(null,
                         null,
                         []);

        // single middleware
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        // delegate middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>));
                             observations.QueriesFromMiddlewares.Add(ctx.Query);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Query, ctx.CancellationToken);
                         }),
                         null,
                         [
                             (typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>));
                             observations.QueriesFromMiddlewares.Add(ctx.Query);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Query, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>));
                             observations.QueriesFromMiddlewares.Add(ctx.Query);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Query, ctx.CancellationToken);
                         }),
                         p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>));
                             observations.QueriesFromMiddlewares.Add(ctx.Query);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Query, ctx.CancellationToken);
                         }),
                         [
                             (typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        // multiple different middlewares
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        // mix delegate and normal middleware
        yield return new(p => p.Use(async ctx =>
                         {
                             await Task.Yield();
                             var observations = ctx.ServiceProvider.GetRequiredService<TestObservations>();
                             observations.MiddlewareTypes.Add(typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>));
                             observations.QueriesFromMiddlewares.Add(ctx.Query);
                             observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
                             observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

                             return await ctx.Next(ctx.Query, ctx.CancellationToken);
                         }).Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(DelegateQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        // same middleware multiple times
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        // added, then removed
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2<TestQuery, TestQueryResponse>>(),
                         null,
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2<TestQuery, TestQueryResponse>>(),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        // multiple times added, then removed
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2<TestQuery, TestQueryResponse>>(),
                         null,
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware2<TestQuery, TestQueryResponse>>(),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        // added on client, added and removed in handler
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware<TestQuery, TestQueryResponse>>(),
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        // added, then removed, then added again
        yield return new(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware<TestQuery, TestQueryResponse>>()
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Without<TestQueryMiddleware<TestQuery, TestQueryResponse>>()
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        // retry middlewares
        yield return new(p => p.Use(new TestQueryRetryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         null,
                         [
                             (typeof(TestQueryRetryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                         ]);

        yield return new(null,
                         p => p.Use(new TestQueryRetryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryRetryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                         ]);

        yield return new(p => p.Use(new TestQueryRetryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         p => p.Use(new TestQueryRetryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>()))
                               .Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())),
                         [
                             (typeof(TestQueryRetryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryRetryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), QueryTransportRole.Client),
                             (typeof(TestQueryRetryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
                             (typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>), QueryTransportRole.Server),
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
                        _ = pipeline.Use(new MutatingTestQueryMiddleware<TestQuery, TestQueryResponse>(obs, cancellationTokensToUse))
                                    .Use(new MutatingTestQueryMiddleware2<TestQuery, TestQueryResponse>(obs, cancellationTokensToUse));
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var response = await handler.Handle(new(0), tokens.CancellationTokens[0]);

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
                    .AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline => pipeline.Use(new ThrowingTestQueryMiddleware<TestQuery, TestQueryResponse>(exception)));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.Handle(new(10)));

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

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.WithPipeline(p => p.Use(new ThrowingTestQueryMiddleware<TestQuery, TestQueryResponse>(exception))).Handle(new(10)));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandlerWithMiddlewares_WhenMiddlewareIsExecuted_ServiceProviderInContextAndPipelineConfigurationIsFromResolutionScope()
    {
        var services = new ServiceCollection();

        IServiceProvider? providerFromHandlerPipelineBuild = null;
        IServiceProvider? providerFromHandlerMiddleware = null;
        IServiceProvider? providerFromClientPipelineBuild = null;
        IServiceProvider? providerFromClientMiddleware = null;

        _ = services.AddConquerorQueryHandler<TestQueryHandler>()
                    .AddTransient<TestObservations>()
                    .AddSingleton<Action<IQueryPipeline<TestQuery, TestQueryResponse>>>(pipeline =>
                    {
                        providerFromHandlerPipelineBuild = pipeline.ServiceProvider;
                        _ = pipeline.Use(ctx =>
                        {
                            providerFromHandlerMiddleware = ctx.ServiceProvider;
                            return ctx.Next(ctx.Query, ctx.CancellationToken);
                        });
                    });

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.WithPipeline(pipeline =>
        {
            providerFromClientPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Query, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromHandlerPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientPipelineBuild, Is.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope1.ServiceProvider));

        _ = await handler2.WithPipeline(pipeline =>
        {
            providerFromClientPipelineBuild = pipeline.ServiceProvider;
            _ = pipeline.Use(ctx =>
            {
                providerFromClientMiddleware = ctx.ServiceProvider;
                return ctx.Next(ctx.Query, ctx.CancellationToken);
            });
        }).Handle(new(10));

        Assert.That(providerFromHandlerPipelineBuild, Is.Not.SameAs(scope1.ServiceProvider));
        Assert.That(providerFromHandlerPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromHandlerMiddleware, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientPipelineBuild, Is.SameAs(scope2.ServiceProvider));
        Assert.That(providerFromClientMiddleware, Is.SameAs(scope2.ServiceProvider));
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

        _ = await handler.WithPipeline(p => p.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .WithPipeline(p => p.Use(new TestQueryMiddleware2<TestQuery, TestQueryResponse>(p.ServiceProvider.GetRequiredService<TestObservations>())))
                         .Handle(new(10));

        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2<TestQuery, TestQueryResponse>), typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>) }));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_WhenHandlerIsCalled_MiddlewareIsCalledWithQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (query, p, cancellationToken) =>
                    {
                        await Task.Yield();
                        var obs = p.GetRequiredService<TestObservations>();
                        obs.QueriesFromHandlers.Add(query);
                        obs.CancellationTokensFromHandlers.Add(cancellationToken);
                        return new(query.Payload + 1);
                    }, pipeline => pipeline.Use(new TestQueryMiddleware<TestQuery, TestQueryResponse>(pipeline.ServiceProvider.GetRequiredService<TestObservations>())))
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.Handle(query);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware<TestQuery, TestQueryResponse>) }));
    }

    [Test]
    public async Task GivenHandlerAndClientPipeline_WhenHandlerIsCalled_TransportTypesInPipelinesAreCorrect()
    {
        var services = new ServiceCollection();
        QueryTransportType? transportTypeFromClient = null;
        QueryTransportType? transportTypeFromHandler = null;

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>(async (query, _, _) =>
        {
            await Task.Yield();
            return new(query.Payload + 1);
        }, pipeline => transportTypeFromHandler = pipeline.TransportType);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.WithPipeline(pipeline => transportTypeFromClient = pipeline.TransportType).Handle(query);

        Assert.That(transportTypeFromClient, Is.EqualTo(new QueryTransportType(InProcessQueryTransportTypeExtensions.TransportName, QueryTransportRole.Client)));
        Assert.That(transportTypeFromHandler, Is.EqualTo(new QueryTransportType(InProcessQueryTransportTypeExtensions.TransportName, QueryTransportRole.Server)));
    }

    [Test]
    public async Task GivenHandlerAndClientPipeline_WhenPipelineIsBeingBuilt_MiddlewaresCanBeEnumerated()
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorQueryHandlerDelegate<TestQuery, TestQueryResponse>((query, _, _) => Task.FromResult<TestQueryResponse>(new(query.Payload + 1)),
                                                                                    pipeline =>
                                                                                    {
                                                                                        var middleware1 = new TestQueryMiddleware<TestQuery, TestQueryResponse>(new());
                                                                                        var middleware2 = new TestQueryMiddleware2<TestQuery, TestQueryResponse>(new());
                                                                                        _ = pipeline.Use(middleware1).Use(middleware2);

                                                                                        Assert.That(pipeline, Has.Count.EqualTo(2));
                                                                                        Assert.That(pipeline, Is.EquivalentTo(new IQueryMiddleware<TestQuery, TestQueryResponse>[] { middleware1, middleware2 }));
                                                                                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.WithPipeline(pipeline =>
                         {
                             var middleware1 = new TestQueryMiddleware<TestQuery, TestQueryResponse>(new());
                             var middleware2 = new TestQueryMiddleware2<TestQuery, TestQueryResponse>(new());
                             _ = pipeline.Use(middleware1).Use(middleware2);

                             Assert.That(pipeline, Has.Count.EqualTo(2));
                             Assert.That(pipeline, Is.EquivalentTo(new IQueryMiddleware<TestQuery, TestQueryResponse>[] { middleware1, middleware2 }));
                         })
                         .Handle(query);
    }

    public sealed record ConquerorMiddlewareFunctionalityTestCase(
        Action<IQueryPipeline<TestQuery, TestQueryResponse>>? ConfigureHandlerPipeline,
        Action<IQueryPipeline<TestQuery, TestQueryResponse>>? ConfigureClientPipeline,
        IReadOnlyCollection<(Type MiddlewareType, QueryTransportRole TransportRole)> ExpectedMiddlewareTypes);

    public sealed record TestQuery(int Payload);

    public sealed record TestQueryResponse(int Payload);

    private sealed class TestQueryHandler(TestObservations observations) : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> Handle(TestQuery query, CancellationToken cancellationToken = default)
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

    private sealed class TestQueryMiddleware<TQuery, TResponse>(TestObservations observations) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryMiddleware2<TQuery, TResponse>(TestObservations observations) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.TransportTypesFromMiddlewares.Add(ctx.TransportType);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryRetryMiddleware<TQuery, TResponse>(TestObservations observations) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
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

    private sealed class MutatingTestQueryMiddleware<TQuery, TResponse>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
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

    private sealed class MutatingTestQueryMiddleware2<TQuery, TResponse>(TestObservations observations, CancellationTokensToUse cancellationTokensToUse) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
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

    private sealed class ThrowingTestQueryMiddleware<TQuery, TResponse>(Exception exception) : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public async Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx)
        {
            await Task.Yield();
            throw exception;
        }
    }

    // only used as a marker for pipeline type check
    private sealed class DelegateQueryMiddleware<TQuery, TResponse> : IQueryMiddleware<TQuery, TResponse>
        where TQuery : class
    {
        public Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx) => throw new NotSupportedException();
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
