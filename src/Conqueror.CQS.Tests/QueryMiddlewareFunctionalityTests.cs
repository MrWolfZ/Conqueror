namespace Conqueror.CQS.Tests;

public sealed class QueryMiddlewareFunctionalityTests
{
    [Test]
    public async Task GivenHandlerWithNoHandlerMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenHandlerWithSingleAppliedHandlerMiddleware_MiddlewareIsCalledWithQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithSingleMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithSingleAppliedHandlerMiddlewareWithParameter_MiddlewareIsCalledWithConfiguration()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithSingleMiddlewareWithParameter>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(10), CancellationToken.None);

        Assert.That(observations.ConfigurationFromMiddlewares, Is.EquivalentTo(new[] { new TestQueryMiddlewareConfiguration { Parameter = 10 } }));
    }

    [Test]
    public async Task GivenHandlerWithMultipleAppliedHandlerMiddlewares_MiddlewaresAreCalledWithQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2) }));
    }

    [Test]
    public async Task GivenHandlerWithSameMiddlewareAppliedMultipleTimes_MiddlewareIsCalledWithQueryMultipleTimes()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware2) }));
    }

    [Test]
    public async Task GivenHandlerWithAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Use<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Without<TestQueryMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Use<TestQueryMiddleware2>()
                                    .Without<TestQueryMiddleware, TestQueryMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware2) }));
    }

    [Test]
    public async Task GivenHandlerWithMultipleAppliedAndThenRemovedMiddleware_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Use<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Without<TestQueryMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware), typeof(TestQueryMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithMultipleAppliedAndThenRemovedMiddlewareWithConfiguration_MiddlewareIsNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Use<TestQueryMiddleware2>()
                                    .Without<TestQueryMiddleware, TestQueryMiddlewareConfiguration>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddleware_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware2>()
                                    .Without<TestQueryMiddleware2>()
                                    .Use<TestQueryMiddleware2>();
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware2) }));
    }

    [Test]
    public async Task GivenPipelineWithExistingMiddlewareWithConfiguration_WhenAddingSameMiddlewareAgainAfterRemovingPreviousMiddleware_MiddlewareIsCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton<Action<IQueryPipelineBuilder>>(pipeline =>
                    {
                        _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                                    .Without<TestQueryMiddleware, TestQueryMiddlewareConfiguration>()
                                    .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new());
                    });

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
    }

    [Test]
    public async Task GivenHandlerWithRetryMiddleware_MiddlewaresAreCalledMultipleTimesWithQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithRetryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryRetryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query, query, query, query, query }));
        Assert.That(observations.MiddlewareTypes,
                    Is.EquivalentTo(new[]
                    {
                        typeof(TestQueryRetryMiddleware), typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2), typeof(TestQueryMiddleware), typeof(TestQueryMiddleware2),
                    }));
    }

    [Test]
    public async Task GivenHandlerWithPipelineConfigurationMethodWithoutPipelineConfigurationInterface_MiddlewaresAreNotCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithPipelineConfigurationWithoutPipelineConfigurationInterface>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var query = new TestQuery(10);

        _ = await handler.ExecuteQuery(query, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.Empty);
        Assert.That(observations.MiddlewareTypes, Is.Empty);
    }

    [Test]
    public async Task GivenCancellationToken_MiddlewaresReceiveCancellationTokenWhenCalled()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleMiddlewares>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        using var tokenSource = new CancellationTokenSource();

        _ = await handler.ExecuteQuery(new(10), tokenSource.Token);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(new[] { tokenSource.Token, tokenSource.Token }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheQuery()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleMutatingMiddlewares>()
                    .AddConquerorQueryMiddleware<MutatingTestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<MutatingTestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(0), CancellationToken.None);

        var query1 = new TestQuery(0);
        var query2 = new TestQuery(1);
        var query3 = new TestQuery(3);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { query1, query2 }));
        Assert.That(observations.QueriesFromHandlers, Is.EquivalentTo(new[] { query3 }));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheResponse()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false), new(false), new(false) } };

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleMutatingMiddlewares>()
                    .AddConquerorQueryMiddleware<MutatingTestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<MutatingTestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var response = await handler.ExecuteQuery(new(0), CancellationToken.None);

        var response1 = new TestQueryResponse(0);
        var response2 = new TestQueryResponse(1);
        var response3 = new TestQueryResponse(3);

        Assert.That(observations.ResponsesFromMiddlewares, Is.EquivalentTo(new[] { response1, response2 }));
        Assert.That(response, Is.EqualTo(response3));
    }

    [Test]
    public async Task GivenMiddlewares_MiddlewaresCanChangeTheCancellationTokens()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var tokens = new CancellationTokensToUse { CancellationTokens = { new(false), new(false), new(false) } };

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithMultipleMutatingMiddlewares>()
                    .AddConquerorQueryMiddleware<MutatingTestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<MutatingTestQueryMiddleware2>()
                    .AddSingleton(observations)
                    .AddSingleton(tokens);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler.ExecuteQuery(new(0), tokens.CancellationTokens[0]);

        Assert.That(observations.CancellationTokensFromMiddlewares, Is.EquivalentTo(tokens.CancellationTokens.Take(2)));
        Assert.That(observations.CancellationTokensFromHandlers, Is.EquivalentTo(new[] { tokens.CancellationTokens[2] }));
    }

    [Test]
    public async Task GivenPipelineThatResolvesScopedService_EachExecutionGetsInstanceFromScope()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();
        var observedInstances = new List<TestService>();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddScoped<TestService>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipelineBuilder>>(pipeline => observedInstances.Add(pipeline.ServiceProvider.GetRequiredService<TestService>()));

        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var handler1 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();
        var handler3 = scope1.ServiceProvider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        _ = await handler1.ExecuteQuery(new(10), CancellationToken.None);
        _ = await handler2.ExecuteQuery(new(10), CancellationToken.None);
        _ = await handler3.ExecuteQuery(new(10), CancellationToken.None);

        Assert.That(observedInstances, Has.Count.EqualTo(3));
        Assert.That(observedInstances[1], Is.Not.SameAs(observedInstances[0]));
        Assert.That(observedInstances[2], Is.SameAs(observedInstances[0]));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddleware_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipelineBuilder>>(pipeline => pipeline.Use<TestQueryMiddleware2>());

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestQueryMiddleware2)));
    }

    [Test]
    public void GivenPipelineThatUsesUnregisteredMiddlewareWithConfiguration_PipelineExecutionThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var observations = new TestObservations();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithoutMiddlewares>()
                    .AddSingleton(observations);

        _ = services.AddSingleton<Action<IQueryPipelineBuilder>>(pipeline => pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()));

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

        Assert.That(exception?.Message, Contains.Substring("trying to use unregistered middleware type"));
        Assert.That(exception?.Message, Contains.Substring(nameof(TestQueryMiddleware)));
    }

    [Test]
    public void GivenMiddlewareThatThrows_InvocationThrowsSameException()
    {
        var services = new ServiceCollection();
        var exception = new Exception();

        _ = services.AddConquerorQueryHandler<TestQueryHandlerWithThrowingMiddleware>()
                    .AddConquerorQueryMiddleware<ThrowingTestQueryMiddleware>()
                    .AddSingleton(exception);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var thrownException = Assert.ThrowsAsync<Exception>(() => handler.ExecuteQuery(new(10), CancellationToken.None));

        Assert.That(thrownException, Is.SameAs(exception));
    }

    [Test]
    public async Task GivenHandlerDelegateWithSingleAppliedMiddleware_MiddlewareIsCalledWithQuery()
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
                                                                                    },
                                                                                    pipeline => pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new()))
                    .AddConquerorQueryMiddleware<TestQueryMiddleware>()
                    .AddConquerorQueryMiddleware<TestQueryMiddleware2>()
                    .AddSingleton(observations);

        var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IQueryHandler<TestQuery, TestQueryResponse>>();

        var command = new TestQuery(10);

        _ = await handler.ExecuteQuery(command, CancellationToken.None);

        Assert.That(observations.QueriesFromMiddlewares, Is.EquivalentTo(new[] { command }));
        Assert.That(observations.MiddlewareTypes, Is.EquivalentTo(new[] { typeof(TestQueryMiddleware) }));
    }

    [Test]
    public void InvalidMiddlewares()
    {
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryMiddleware<TestQueryMiddlewareWithMultipleInterfaces>());
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryMiddleware<TestQueryMiddlewareWithMultipleInterfaces>(_ => new()));
        _ = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddConquerorQueryMiddleware(new TestQueryMiddlewareWithMultipleInterfaces()));
    }

    private sealed record TestQuery(int Payload);

    private sealed record TestQueryResponse(int Payload);

    private sealed class TestQueryHandlerWithSingleMiddleware : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithSingleMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(query.Payload + 1);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new());
        }
    }

    private sealed class TestQueryHandlerWithSingleMiddlewareWithParameter : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithSingleMiddlewareWithParameter(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(query.Payload + 1);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestQueryHandlerWithMultipleMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithMultipleMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                        .Use<TestQueryMiddleware2>();
        }
    }

    private sealed class TestQueryHandlerWithoutMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithoutMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            pipeline.ServiceProvider.GetService<Action<IQueryPipelineBuilder>>()?.Invoke(pipeline);
        }
    }

    private sealed class TestQueryHandlerWithRetryMiddleware : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestQueryRetryMiddleware>()
                        .Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new())
                        .Use<TestQueryMiddleware2>();
        }
    }

    private sealed class TestQueryHandlerWithMultipleMutatingMiddlewares : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        private readonly TestObservations observations;

        public TestQueryHandlerWithMultipleMutatingMiddlewares(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            observations.QueriesFromHandlers.Add(query);
            observations.CancellationTokensFromHandlers.Add(cancellationToken);
            return new(0);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<MutatingTestQueryMiddleware>()
                        .Use<MutatingTestQueryMiddleware2>();
        }
    }

    private sealed class TestQueryHandlerWithPipelineConfigurationWithoutPipelineConfigurationInterface : IQueryHandler<TestQuery, TestQueryResponse>
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }

        // ReSharper disable once UnusedMember.Local
        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<TestQueryMiddleware, TestQueryMiddlewareConfiguration>(new() { Parameter = 10 });
        }
    }

    private sealed class TestQueryHandlerWithThrowingMiddleware : IQueryHandler<TestQuery, TestQueryResponse>, IConfigureQueryPipeline
    {
        public async Task<TestQueryResponse> ExecuteQuery(TestQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return new(0);
        }

        public static void ConfigurePipeline(IQueryPipelineBuilder pipeline)
        {
            _ = pipeline.Use<ThrowingTestQueryMiddleware, TestQueryMiddlewareConfiguration>(new());
        }
    }

    private sealed record TestQueryMiddlewareConfiguration
    {
        public int Parameter { get; set; }
    }

    private sealed class TestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
    {
        private readonly TestObservations observations;

        public TestQueryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);
            observations.ConfigurationFromMiddlewares.Add(ctx.Configuration);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryMiddleware2 : IQueryMiddleware
    {
        private readonly TestObservations observations;

        public TestQueryMiddleware2(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class TestQueryRetryMiddleware : IQueryMiddleware
    {
        private readonly TestObservations observations;

        public TestQueryRetryMiddleware(TestObservations observations)
        {
            this.observations = observations;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            _ = await ctx.Next(ctx.Query, ctx.CancellationToken);
            return await ctx.Next(ctx.Query, ctx.CancellationToken);
        }
    }

    private sealed class MutatingTestQueryMiddleware : IQueryMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestQueryMiddleware(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var query = ctx.Query;

            if (query is TestQuery testQuery)
            {
                query = (TQuery)(object)(testQuery with { Payload = testQuery.Payload + 1 });
            }

            var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[1]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestQueryResponse testQueryResponse)
            {
                response = (TResponse)(object)(testQueryResponse with { Payload = testQueryResponse.Payload + 2 });
            }

            return response;
        }
    }

    private sealed class MutatingTestQueryMiddleware2 : IQueryMiddleware
    {
        private readonly CancellationTokensToUse cancellationTokensToUse;
        private readonly TestObservations observations;

        public MutatingTestQueryMiddleware2(TestObservations observations, CancellationTokensToUse cancellationTokensToUse)
        {
            this.observations = observations;
            this.cancellationTokensToUse = cancellationTokensToUse;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class
        {
            await Task.Yield();
            observations.MiddlewareTypes.Add(GetType());
            observations.QueriesFromMiddlewares.Add(ctx.Query);
            observations.CancellationTokensFromMiddlewares.Add(ctx.CancellationToken);

            var query = ctx.Query;

            if (query is TestQuery testQuery)
            {
                query = (TQuery)(object)(testQuery with { Payload = testQuery.Payload + 2 });
            }

            var response = await ctx.Next(query, cancellationTokensToUse.CancellationTokens[2]);

            observations.ResponsesFromMiddlewares.Add(response!);

            if (response is TestQueryResponse testQueryResponse)
            {
                response = (TResponse)(object)(testQueryResponse with { Payload = testQueryResponse.Payload + 1 });
            }

            return response;
        }
    }

    private sealed class ThrowingTestQueryMiddleware : IQueryMiddleware<TestQueryMiddlewareConfiguration>
    {
        private readonly Exception exception;

        public ThrowingTestQueryMiddleware(Exception exception)
        {
            this.exception = exception;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
            where TQuery : class
        {
            await Task.Yield();
            throw exception;
        }
    }

    private sealed class TestQueryMiddlewareWithMultipleInterfaces : IQueryMiddleware<TestQueryMiddlewareConfiguration>,
                                                                     IQueryMiddleware
    {
        public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class =>
            throw new InvalidOperationException("this middleware should never be called");

        public Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TestQueryMiddlewareConfiguration> ctx)
            where TQuery : class =>
            throw new InvalidOperationException("this middleware should never be called");
    }

    private sealed class TestObservations
    {
        public List<Type> MiddlewareTypes { get; } = new();

        public List<object> QueriesFromHandlers { get; } = new();

        public List<object> QueriesFromMiddlewares { get; } = new();

        public List<object> ResponsesFromMiddlewares { get; } = new();

        public List<CancellationToken> CancellationTokensFromHandlers { get; } = new();

        public List<CancellationToken> CancellationTokensFromMiddlewares { get; } = new();

        public List<object> ConfigurationFromMiddlewares { get; } = new();
    }

    private sealed class CancellationTokensToUse
    {
        public List<CancellationToken> CancellationTokens { get; } = new();
    }

    private sealed class TestService
    {
    }
}
